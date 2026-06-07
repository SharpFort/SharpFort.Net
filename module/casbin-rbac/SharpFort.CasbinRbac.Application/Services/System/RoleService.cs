using Casbin;
using System.Globalization;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using SharpFort.CasbinRbac.Application.Extensions;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Role;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.User;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Role服务实现
    /// </summary>
    public class RoleService : SfCrudAppService<Role, RoleGetOutputDto, RoleGetListOutputDto, Guid,
            RoleGetListInputVo, RoleCreateInputVo, RoleUpdateInputVo>,
        IRoleService
    {
        private readonly IEnforcer _enforcer;
        private readonly ISqlSugarRepository<Menu, Guid> _menuRepository;
        private readonly ICasbinPolicyManager _casbinPolicyManager;
        private readonly ISqlSugarRepository<User, Guid> _userRepository;

        public RoleService(RoleManager roleManager, ISqlSugarRepository<RoleDepartment> roleDeptRepository,
            ISqlSugarRepository<UserRole> userRoleRepository,
            ISqlSugarRepository<Role, Guid> repository,
            IEnforcer enforcer,
            ISqlSugarRepository<Menu, Guid> menuRepository,
            ICasbinPolicyManager casbinPolicyManager,
            ISqlSugarRepository<User, Guid> userRepository,
            IDistributedCache distributedCache) : base(repository)
        {
            _distributedCache = distributedCache;
            (_roleManager, _roleDeptRepository, _userRoleRepository, _repository, _enforcer, _menuRepository, _casbinPolicyManager, _userRepository) =
                (roleManager, roleDeptRepository, userRoleRepository, repository, enforcer, menuRepository, casbinPolicyManager, userRepository);
        }

        private readonly IDistributedCache _distributedCache;
        private readonly ISqlSugarRepository<Role, Guid> _repository;

        // ========== 缓存版本控制 ==========
        private static long _roleSchemaVersion = 1;

        private void InvalidateRoleCache()
        {
            Interlocked.Increment(ref _roleSchemaVersion);
        }

        private static string GetRoleCachedKeyPrefix()
        {
            long ver = Interlocked.Read(ref _roleSchemaVersion);
            return $"Role:v{ver}:";
        }

        // ========== 重写下拉列表查询（Redis 缓存） ==========
        public override async Task<PagedResultDto<RoleGetListOutputDto>> GetSelectDataListAsync(
            string? keywords = null)
        {
            string cacheKey = $"{GetRoleCachedKeyPrefix()}Select:ALL";

            var cached = await _distributedCache.GetFromCacheAsync<PagedResultDto<RoleGetListOutputDto>>(cacheKey);
            if (cached is not null) return cached;

            var result = await base.GetSelectDataListAsync(keywords);

            var options = result.TotalCount == 0
                ? SfDistributedCacheExtensions.ShortCacheOptions
                : SfDistributedCacheExtensions.DefaultCacheOptions;
            await _distributedCache.SetCacheAsync(cacheKey, result, options);
            return result;
        }
        private RoleManager _roleManager { get; set; }

        private readonly ISqlSugarRepository<RoleDepartment> _roleDeptRepository;

        private readonly ISqlSugarRepository<UserRole> _userRoleRepository;

        public async Task UpdateDataScopeAsync(UpdateDataScopeInput input)
        {
            //只有自定义的需要特殊处理
            if (input.DataScope == DataScope.CUSTOM)
            {
                await _roleDeptRepository.DeleteAsync(x => x.RoleId == input.RoleId);
                List<RoleDepartment> insertEntities = [.. (input.DepartmentIds ?? []).Select(x => new RoleDepartment { DepartmentId = x, RoleId = input.RoleId })];
                await _roleDeptRepository.InsertRangeAsync(insertEntities);
            }

            Role entity = new() { DataScope = input.DataScope };
            EntityHelper.TrySetId(entity, () => input.RoleId);
            await _repository._Db.Updateable(entity).UpdateColumns(x => x.DataScope).ExecuteCommandAsync();
            InvalidateRoleCache();
        }

        public override async Task<PagedResultDto<RoleGetListOutputDto>> GetListAsync(RoleGetListInputVo input)
        {
            RefAsync<int> total = 0;

            List<Role> entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.RoleCode),
                    x => x.RoleCode!.Contains(input.RoleCode!))
                .WhereIF(!string.IsNullOrEmpty(input.RoleName), x => x.RoleName!.Contains(input.RoleName!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<RoleGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<RoleGetOutputDto> CreateAsync(RoleCreateInputVo input)
        {
            // B-07: 增加租户过滤
            Guid? currentTenantId = CurrentTenant.Id;
            bool isExist = await _repository._DbQueryable
                .Where(x => x.TenantId == currentTenantId)
                .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }

            Role entity = await MapToEntityAsync(input);
            await _repository.InsertAsync(entity);

            await _roleManager.GiveRoleSetMenuAsync([entity.Id], input.MenuIds ?? []);

            RoleGetOutputDto outputDto = await MapToGetOutputDtoAsync(entity);

            InvalidateRoleCache();
            return outputDto;
        }

        /// <summary>
        /// 修改角色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<RoleGetOutputDto> UpdateAsync(Guid id, RoleUpdateInputVo input)
        {
            Role entity = await _repository.GetByIdAsync(id);

            // MISSING-04: 保护超级管理员角色编码不被修改
            if (string.Equals(entity.RoleCode, UserConst.AdminRolesCode, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(input.RoleCode, UserConst.AdminRolesCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("超级管理员角色编码不允许修改");
            }

            // B-07: 增加租户过滤
            bool isExist = await _repository._DbQueryable
                .Where(x => x.Id != entity.Id)
                .Where(x => x.TenantId == entity.TenantId)
                .AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }

            string oldRoleCode = entity.RoleCode!;

            await MapToEntityAsync(input, entity);

            // B-05: 先更新业务表（最关键）
            await _repository.UpdateAsync(entity);

            // R-05: 如果 RoleCode 变更了，使用迁移而非删除（避免 g-rules 丢失）
            if (oldRoleCode != entity.RoleCode)
            {
                await _casbinPolicyManager.MigrateRoleCodeAsync(oldRoleCode, entity.RoleCode!, entity.TenantId);
            }

            // 最后更新菜单策略（SetRolePermissionsAsync 会覆盖旧的 p 规则）
            await _roleManager.GiveRoleSetMenuAsync([id], input.MenuIds ?? []);

            RoleGetOutputDto dto = await MapToGetOutputDtoAsync(entity);
            InvalidateRoleCache();
            return dto;
        }



        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [Route("role/{id}/{state}")]
        public async Task<RoleGetOutputDto> UpdateStateAsync([FromRoute] Guid id, [FromRoute] bool state)
        {
            Role entity = await _repository.GetByIdAsync(id) ?? throw new UserFriendlyException("角色未存在");

            // QA5-CRITICAL-02: 禁止禁用超级管理员角色
            if (!state && string.Equals(entity.RoleCode, UserConst.AdminRolesCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("超级管理员角色不允许禁用");
            }

            entity.State = state;
            await _repository.UpdateAsync(entity);
            InvalidateRoleCache();
            return await MapToGetOutputDtoAsync(entity);
        }


        /// <summary>
        /// 获取角色下的用户
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="input"></param>
        /// <param name="isAllocated">是否在该角色下</param>
        /// <returns></returns>
        [Route("role/auth-user/{roleId}/{isAllocated}")]
        public async Task<PagedResultDto<UserGetListOutputDto>> GetAuthUserByRoleIdAsync([FromRoute] Guid roleId,
            [FromRoute] bool isAllocated, [FromQuery] RoleAuthUserGetListInput input)
        {
            PagedResultDto<UserGetListOutputDto> output = isAllocated
                ? await GetAllocatedAuthUserByRoleIdAsync(roleId, input)
                //角色下未授权用户
                : await GetNotAllocatedAuthUserByRoleIdAsync(roleId, input);
            //角色下已授权用户

            return output;
        }

        private async Task<PagedResultDto<UserGetListOutputDto>> GetAllocatedAuthUserByRoleIdAsync(Guid roleId,
            RoleAuthUserGetListInput input)
        {
            RefAsync<int> total = 0;
            List<UserGetListOutputDto> output = await _userRoleRepository._DbQueryable
                .LeftJoin<User>((ur, u) => ur.UserId == u.Id && ur.RoleId == roleId)
                .Where((ur, u) => ur.RoleId == roleId)
                .WhereIF(!string.IsNullOrEmpty(input.UserName), (ur, u) => u.UserName!.Contains(input.UserName!))
                .WhereIF(input.Phone is not null, (ur, u) => u.Phone!.Value.ToString(CultureInfo.InvariantCulture).Contains(input.Phone!.Value.ToString(CultureInfo.InvariantCulture)))
                .Select((ur, u) => new UserGetListOutputDto { Id = u.Id }, true)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<UserGetListOutputDto>(total, output);
        }

        private async Task<PagedResultDto<UserGetListOutputDto>> GetNotAllocatedAuthUserByRoleIdAsync(Guid roleId,
            RoleAuthUserGetListInput input)
        {
            RefAsync<int> total = 0;
            List<User> entities = await _userRoleRepository._Db.Queryable<User>()
                .Where(u => SqlFunc.Subqueryable<UserRole>().Where(x => x.RoleId == roleId)
                    .Where(x => x.UserId == u.Id).NotAny())
                .WhereIF(!string.IsNullOrEmpty(input.UserName), u => u.UserName!.Contains(input.UserName!))
                .WhereIF(input.Phone is not null, u => u.Phone!.Value.ToString(CultureInfo.InvariantCulture).Contains(input.Phone!.Value.ToString(CultureInfo.InvariantCulture)))
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            List<UserGetListOutputDto> output = entities.Adapt<List<UserGetListOutputDto>>();
            return new PagedResultDto<UserGetListOutputDto>(total, output);
        }


        /// <summary>
        /// 批量给用户授权
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task CreateAuthUserAsync([FromBody] RoleAuthUserCreateOrDeleteInput input)
        {
            List<UserRole> userRoleEntities = [.. input.UserIds.Select(u => new UserRole { RoleId = input.RoleId, UserId = u })];
            await _userRoleRepository.InsertRangeAsync(userRoleEntities);

            // Casbin 同步：添加用户角色关联 (g) (使用 CasbinPolicyManager 双写同步机制)
            Role role = await _repository.GetByIdAsync(input.RoleId) ?? throw new UserFriendlyException("角色不存在");
            List<User> users = await _userRepository.GetListAsync(u => input.UserIds.Contains(u.Id));
            foreach (User user in users)
            {
                await _casbinPolicyManager.AddRoleForUserAsync(user, role);
            }

            // /* 原代码注释保留 */
            // string domain = "default";
            // 
            // // 查询 RoleCode
            // var role = await _repository.GetByIdAsync(input.RoleId);
            // var roleCode = role?.RoleCode ?? throw new UserFriendlyException("角色不存在");
            // 
            // var policies = input.UserIds.Select(userId => new[] { userId.ToString(), roleCode, domain }).ToList();
            // await _enforcer.AddGroupingPoliciesAsync(policies);
            // await _enforcer.SavePolicyAsync();
        }


        /// <summary>
        /// 批量取消授权
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task DeleteAuthUserAsync([FromBody] RoleAuthUserCreateOrDeleteInput input)
        {
            await _userRoleRepository._Db.Deleteable<UserRole>().Where(x => x.RoleId == input.RoleId)
                .Where(x => input.UserIds.Contains(x.UserId))
                .ExecuteCommandAsync();

            // Casbin 同步：移除用户角色关联 (使用 CasbinPolicyManager 双写同步机制)
            Role role = await _repository.GetByIdAsync(input.RoleId) ?? throw new UserFriendlyException("角色不存在");
            List<User> users = await _userRepository.GetListAsync(u => input.UserIds.Contains(u.Id));
            foreach (User user in users)
            {
                await _casbinPolicyManager.RemoveRoleForUserAsync(user, role);
            }

            // /* 原代码注释保留 */
            // string domain = "default";
            // 
            // // 查询 RoleCode
            // var role = await _repository.GetByIdAsync(input.RoleId);
            // var roleCode = role?.RoleCode ?? throw new UserFriendlyException("角色不存在");
            // 
            // var policies = input.UserIds.Select(userId => new[] { userId.ToString(), roleCode, domain }).ToList();
            // await _enforcer.RemoveGroupingPoliciesAsync(policies);
            // await _enforcer.SavePolicyAsync();
        }

        public override async Task DeleteAsync(IEnumerable<Guid> ids)
        {
            List<Role> roles = await _repository.GetListAsync(x => ids.Contains(x.Id));

            // QA5-CRITICAL-02: 禁止删除超级管理员角色
            if (roles.Any(r => string.Equals(r.RoleCode, UserConst.AdminRolesCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserFriendlyException("超级管理员角色不允许删除");
            }

            await base.DeleteAsync(ids);
            InvalidateRoleCache();

            // 物理删除角色后，清理与该角色绑定的所有 Casbin p规则与g规则
            foreach (Role role in roles)
            {
                await _casbinPolicyManager.CleanRolePoliciesAsync(role);
            }
        }
    }
}