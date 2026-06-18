using System.Text.Json;
using SqlSugar;
using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    public class MenuService(
        ISqlSugarRepository<Menu, Guid> repository,
        ISqlSugarRepository<RoleMenu> roleMenuRepository,
        ICasbinPolicyManager casbinPolicyManager,
        ISqlSugarRepository<Role, Guid> roleRepository,
        IDistributedCache distributedCache, // ABP 统一缓存抽象，支持 MemoryCache 和 Redis 自由切换
        IOptions<CasbinOptions> casbinOptions)
        : SfCrudAppService<Menu, MenuGetOutputDto, MenuGetListOutputDto, Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>(repository),
          IMenuService
    {
        private readonly ISqlSugarRepository<Menu, Guid> _repository = repository;
        private readonly ISqlSugarRepository<RoleMenu> _roleMenuRepository = roleMenuRepository;
        private readonly ICasbinPolicyManager _casbinPolicyManager = casbinPolicyManager;
        private readonly ISqlSugarRepository<Role, Guid> _roleRepository = roleRepository;
        private readonly IDistributedCache _distributedCache = distributedCache;
        private readonly string _adminRoleCode = casbinOptions.Value.SuperAdminRoleCode ?? UserConst.AdminRolesCode;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ================= 极致无锁、无竞态的版本号控制 =================
        private static long _menuSchemaVersion = 1;

        // 原子自增版本号，使所有缓存瞬间失效 (O(1) 级)
        private void InvalidateMenuCache()
        {
            Interlocked.Increment(ref _menuSchemaVersion);
        }

        // 获取当前版本拼接的缓存 Key 前缀
        private static string GetCachedKeyPrefix()
        {
            long currentVersion = Interlocked.Read(ref _menuSchemaVersion);
            return $"Menuv{currentVersion}:";
        }

        // ================= 缓存辅助方法 =================
        private static readonly DistributedCacheEntryOptions _defaultCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        private static readonly DistributedCacheEntryOptions _shortCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// F-11: ABP 风格 ApiUrl 校验 — 验证 URL 是否符合 ABP Auto API 路由约定
        /// 校验失败抛出 UserFriendlyException，确保 Casbin keyMatch2 能正确匹配
        /// </summary>
        private static void ValidateApiUrl(string? apiUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl)) return;

            // 1. ABP Auto API 路由固定以 /api/app/ 开头
            if (!apiUrl.StartsWith("/api/", StringComparison.Ordinal))
            {
                throw new UserFriendlyException(
                    $"ApiUrl 必须以 /api/ 开头（ABP Auto API 路由约定），当前值: {apiUrl}");
            }

            // 2. URL 必须全部小写（Casbin keyMatch2 大小写敏感，ABP 约定自动转 kebab-case 小写）
            if (apiUrl.Any(char.IsUpper))
            {
                throw new UserFriendlyException(
                    $"ApiUrl 必须全部小写（ABP 约定 + Casbin 大小写敏感），当前值: {apiUrl}");
            }

            // 3. 不支持 {param} 格式（keyMatch2 使用 :param 语法）
            if (apiUrl.Contains('{'))
            {
                throw new UserFriendlyException(
                    $"ApiUrl 不支持 {{param}} 格式，请使用 :param 或 * 通配符。示例: /api/app/user/:id");
            }
        }

        private async Task<T?> GetFromCacheAsync<T>(string key)
        {
            string? json = await _distributedCache.GetStringAsync(key);
            if (json is null) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private async Task SetCacheAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null)
        {
            string json = JsonSerializer.Serialize(value, _jsonOptions);
            await _distributedCache.SetStringAsync(key, json, options ?? _defaultCacheOptions);
        }

        // ================= 1. 本地缓存预热机制 =================
        public async Task WarmupCacheAsync()
        {
            var sources = Enum.GetValues<MenuSource>();
            bool?[] states = [null, true, false];

            foreach (var source in sources)
            {
                foreach (var state in states)
                {
                    await GetListAsync(new MenuGetListInputVo { MenuSource = source, State = state });
                }
            }
        }

        // ================= 2. 读接口重构（列表/权限过滤） =================
        public override async Task<PagedResultDto<MenuGetListOutputDto>> GetListAsync(MenuGetListInputVo input)
        {
            string keyPrefix = GetCachedKeyPrefix();
            string searchName = input.MenuName ?? "__ALL__";
            string stateKey = input.State?.ToString() ?? "all";
            string cacheKey = $"{keyPrefix}List:{input.MenuSource}:{stateKey}:{searchName}";

            PagedResultDto<MenuGetListOutputDto>? cached = await GetFromCacheAsync<PagedResultDto<MenuGetListOutputDto>>(cacheKey);
            if (cached is not null) return cached;

            List<Menu> entities = await _repository._DbQueryable
                        .WhereIF(!string.IsNullOrEmpty(input.MenuName), x => x.MenuName!.Contains(input.MenuName!))
                        .WhereIF(input.State is not null, x => x.State == input.State)
                        .Where(x => x.MenuSource == input.MenuSource)
                        .OrderBy(x => x.OrderNum)
                        .OrderBy(x => x.CreationTime)
                        .ToListAsync();

            int total = entities.Count;
            var dtos = await MapToGetListOutputDtosAsync(entities);
            var result = new PagedResultDto<MenuGetListOutputDto>(total, dtos);

            await SetCacheAsync(cacheKey, result);
            return result;
        }

        public async Task<List<MenuGetListOutputDto>> GetListRoleIdAsync(Guid roleId)
        {
            string keyPrefix = GetCachedKeyPrefix();
            string cacheKey = $"{keyPrefix}RoleList:{roleId}";

            List<MenuGetListOutputDto>? cached = await GetFromCacheAsync<List<MenuGetListOutputDto>>(cacheKey);
            if (cached is not null) return cached;

            List<Menu> entities = await _repository._DbQueryable
                .Where(m => SqlFunc.Subqueryable<RoleMenu>().Where(rm => rm.RoleId == roleId && rm.MenuId == m.Id).Any())
                .ToListAsync();

            var result = await MapToGetListOutputDtosAsync(entities);
            await SetCacheAsync(cacheKey, result);
            return result;
        }

        // ================= 3. 读接口重构（详情缓存） =================
        public override async Task<MenuGetOutputDto> GetAsync(Guid id)
        {
            string keyPrefix = GetCachedKeyPrefix();
            string cacheKey = $"{keyPrefix}Detail:{id}";

            MenuGetOutputDto? cached = await GetFromCacheAsync<MenuGetOutputDto>(cacheKey);
            if (cached is not null) return cached;

            try
            {
                var dto = await base.GetAsync(id);
                await SetCacheAsync(cacheKey, dto);
                return dto;
            }
            catch (EntityNotFoundException)
            {
                // 缓存 sentinel 空对象，配合短 TTL 防止缓存穿透
                await SetCacheAsync(cacheKey, new MenuGetOutputDto(), _shortCacheOptions);
                throw;
            }
        }

        // ================= 4. 写接口重构（单条新增） =================
        public override async Task<MenuGetOutputDto> CreateAsync(MenuCreateInputVo input)
        {
            return await CreateInternalAsync(input, invalidateCache: true, associateAdminRole: true);
        }

        /// <summary>
        /// 内部创建方法
        /// </summary>
        /// <param name="associateAdminRole">是否关联超管 RoleMenu。单条创建时为 true，批量导入时由外部统一处理</param>
        private async Task<MenuGetOutputDto> CreateInternalAsync(
            MenuCreateInputVo input, bool invalidateCache, bool associateAdminRole = true)
        {
            if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
            {
                input.ApiMethod = "GET";
            }

            if (!string.IsNullOrWhiteSpace(input.ApiUrl))
            {
                ValidateApiUrl(input.ApiUrl);
                input.ApiUrl = input.ApiUrl.ToLowerInvariant();
            }

            if (!string.IsNullOrEmpty(input.ApiMethod))
            {
                input.ApiMethod = input.ApiMethod.ToUpper(CultureInfo.InvariantCulture);
            }

            var result = await base.CreateAsync(input);

            // 将新菜单关联给超管（仅 RoleMenu，不碰 Casbin — 超管已有 *,* 通配符）
            if (associateAdminRole)
            {
                Role? adminRole = await _roleRepository.GetFirstAsync(
                    r => r.RoleCode == _adminRoleCode);
                if (adminRole != null)
                {
                    await _roleMenuRepository.InsertAsync(
                        new RoleMenu { RoleId = adminRole.Id, MenuId = result.Id });
                    // 注意：不调用 SetRolePermissionsAsync — 超管已有 *,* 通配符
                }
            }

            if (invalidateCache)
            {
                InvalidateMenuCache();
            }

            return result;
        }

        // ================= 5. 写接口重构（批量导入优化） =================
        public override async Task PostImportExcelAsync(List<MenuCreateInputVo> input)
        {
            List<Guid> newMenuIds = new();
            try
            {
                foreach (var item in input)
                {
                    // 批量导入时不逐个关联超管（associateAdminRole: false）
                    var result = await CreateInternalAsync(item, invalidateCache: false, associateAdminRole: false);
                    newMenuIds.Add(result.Id);
                }

                // 批量结束后一次性插入所有 RoleMenu 关联
                if (newMenuIds.Count > 0)
                {
                    Role? adminRole = await _roleRepository.GetFirstAsync(
                        r => r.RoleCode == _adminRoleCode);
                    if (adminRole != null)
                    {
                        List<RoleMenu> roleMenus = newMenuIds
                            .Select(id => new RoleMenu { RoleId = adminRole.Id, MenuId = id })
                            .ToList();
                        await _roleMenuRepository.InsertRangeAsync(roleMenus);
                    }
                }
            }
            finally
            {
                InvalidateMenuCache();
            }
        }

        // ================= 6. 写接口重构（修改菜单） =================
        public override async Task<MenuGetOutputDto> UpdateAsync(Guid id, MenuUpdateInputVo input)
        {
            if (!string.IsNullOrWhiteSpace(input.ApiUrl) && string.IsNullOrWhiteSpace(input.ApiMethod))
            {
                input.ApiMethod = "GET";
            }

            if (!string.IsNullOrWhiteSpace(input.ApiUrl))
            {
                ValidateApiUrl(input.ApiUrl);
                input.ApiUrl = input.ApiUrl.ToLowerInvariant();
            }

            Menu oldMenu = await _repository.GetByIdAsync(id);
            if (oldMenu == null)
            {
                throw new EntityNotFoundException(typeof(Menu), id);
            }

            bool isApiChanged = (oldMenu.ApiUrl != input.ApiUrl || (oldMenu.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? "") != (input.ApiMethod?.ToUpper(CultureInfo.InvariantCulture) ?? ""));

            await CheckUpdatePolicyAsync();
            await CheckUpdateInputDtoAsync(oldMenu, input);

            await MapToEntityAsync(input, oldMenu);
            await _repository.UpdateAsync(oldMenu, autoSave: true);
            MenuGetOutputDto result = await MapToGetOutputDtoAsync(oldMenu);

            InvalidateMenuCache();

            if (isApiChanged)
            {
                List<Guid> roleIds = await _roleMenuRepository._DbQueryable
                    .Where(x => x.MenuId == id)
                    .Select(x => x.RoleId)
                    .Distinct()
                    .ToListAsync();

                if (roleIds.Count > 0)
                {
                    // F-05: 纵深防御 — 排除超管角色（超管由 *,* 覆盖，不应参与逐菜单 Casbin 同步）
                    List<Role> roles = await _roleRepository.GetListAsync(
                        x => roleIds.Contains(x.Id) && x.RoleCode != _adminRoleCode);

                    var roleMenuMappings = await _roleMenuRepository._DbQueryable
                        .Where(x => roleIds.Contains(x.RoleId))
                        .Select(x => new { x.RoleId, x.MenuId })
                        .ToListAsync();

                    List<Guid> allMenuIds = roleMenuMappings.Select(x => x.MenuId).Distinct().ToList();
                    List<Menu> allMenus = await _repository.GetListAsync(x => allMenuIds.Contains(x.Id));

                    var menuDict = allMenus.ToDictionary(m => m.Id);
                    var roleMenusMap = roleMenuMappings
                        .GroupBy(x => x.RoleId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => menuDict.TryGetValue(x.MenuId, out var m) ? m : null).Where(m => m != null).ToList()
                        );

                    foreach (Role role in roles)
                    {
                        if (roleMenusMap.TryGetValue(role.Id, out List<Menu>? menus))
                        {
                            await _casbinPolicyManager.SetRolePermissionsAsync(role, menus!);
                        }
                    }
                }
            }

            return result;
        }

        // ================= 7. 写接口重构（物理删除） =================
        public override async Task DeleteAsync(IEnumerable<Guid> ids)
        {
            List<Guid> affectedRoleIds = await _roleMenuRepository._DbQueryable
                .Where(x => ids.Contains(x.MenuId))
                .Select(x => x.RoleId)
                .Distinct()
                .ToListAsync();

            await base.DeleteAsync(ids);

            InvalidateMenuCache();

            if (affectedRoleIds.Count > 0)
            {
                // F-05: 纵深防御 — 排除超管角色
                List<Role> roles = await _roleRepository.GetListAsync(
                    x => affectedRoleIds.Contains(x.Id) && x.RoleCode != _adminRoleCode);

                var roleMenuMappings = await _roleMenuRepository._DbQueryable
                    .Where(x => affectedRoleIds.Contains(x.RoleId))
                    .Select(x => new { x.RoleId, x.MenuId })
                    .ToListAsync();

                List<Guid> allMenuIds = roleMenuMappings.Select(x => x.MenuId).Distinct().ToList();
                List<Menu> allMenus = await _repository.GetListAsync(x => allMenuIds.Contains(x.Id));

                var menuDict = allMenus.ToDictionary(m => m.Id);
                var roleMenusMap = roleMenuMappings
                    .GroupBy(x => x.RoleId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => menuDict.TryGetValue(x.MenuId, out var m) ? m : null).Where(m => m != null).ToList()
                    );

                foreach (Role role in roles)
                {
                    List<Menu> menus = roleMenusMap.TryGetValue(role.Id, out List<Menu>? mList) ? mList! : [];
                    await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
                }
            }
        }

        public override async Task<PagedResultDto<MenuGetListOutputDto>> GetSelectDataListAsync(string? keywords = null)
        {
            string keyPrefix = GetCachedKeyPrefix();
            string cacheKey = $"{keyPrefix}Select:{keywords ?? "__ALL__"}";

            PagedResultDto<MenuGetListOutputDto>? cached = await GetFromCacheAsync<PagedResultDto<MenuGetListOutputDto>>(cacheKey);
            if (cached is not null) return cached;

            var result = await base.GetSelectDataListAsync(keywords);
            await SetCacheAsync(cacheKey, result);
            return result;
        }

        public override async Task<Microsoft.AspNetCore.Mvc.IActionResult> GetExportExcelAsync(MenuGetListInputVo input)
        {
            string keyPrefix = GetCachedKeyPrefix();
            string stateKey = input.State?.ToString() ?? "all";
            string cacheKey = $"{keyPrefix}Export:{input.MenuSource}:{stateKey}:{input.MenuName ?? "__ALL__"}";

            byte[]? cached = await GetFromCacheAsync<byte[]>(cacheKey);
            if (cached is not null)
            {
                return new Microsoft.AspNetCore.Mvc.FileContentResult(cached, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = "MenuExport.xlsx"
                };
            }

            var result = await base.GetExportExcelAsync(input);
            if (result is Microsoft.AspNetCore.Mvc.FileContentResult fileResult)
            {
                await SetCacheAsync(cacheKey, fileResult.FileContents);
            }
            return result;
        }
    }
}
