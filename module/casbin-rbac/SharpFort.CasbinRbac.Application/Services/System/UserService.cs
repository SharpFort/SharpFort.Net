using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MiniExcelLibs;
using System.Globalization;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using SharpFort.CasbinRbac.Application.Extensions;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.User;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Role;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;
using SharpFort.CasbinRbac.Domain.Shared.OperLog;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// User服务实现
    /// </summary>
    public class UserService(ISqlSugarRepository<User, Guid> repository, UserManager userManager,
        ICurrentUser currentUser, IDeptService deptService,
        ILocalEventBus localEventBus,
        ICasbinPolicyManager casbinPolicyManager,
        IDistributedCache distributedCache) : SfCrudAppService<User, UserGetOutputDto, UserGetListOutputDto, Guid,
        UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>(repository), IUserService
    {
        protected ILocalEventBus LocalEventBus => LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

        private readonly IDistributedCache _distributedCache = distributedCache;
        private UserManager _userManager { get; set; } = userManager;
        private readonly ISqlSugarRepository<User, Guid> _repository = repository;
        private IDeptService _deptService { get; set; } = deptService;

        private ICurrentUser _currentUser { get; set; } = currentUser;

        private readonly ILocalEventBus _localEventBus = localEventBus;
        private readonly ICasbinPolicyManager _casbinPolicyManager = casbinPolicyManager;

        // ========== 缓存版本控制 ==========
        private static long _userSchemaVersion = 1;

        private void InvalidateUserCache()
        {
            Interlocked.Increment(ref _userSchemaVersion);
        }

        private static string GetUserCachedKeyPrefix()
        {
            long ver = Interlocked.Read(ref _userSchemaVersion);
            return $"User:v{ver}:";
        }

        // ========== 重写下拉列表查询（Redis 缓存） ==========
        public override async Task<PagedResultDto<UserGetListOutputDto>> GetSelectDataListAsync(
            string? keywords = null)
        {
            // 注意：基类忽略 keywords 参数，固定 cacheKey 即可
            string cacheKey = $"{GetUserCachedKeyPrefix()}Select:ALL";

            var cached = await _distributedCache.GetFromCacheAsync<PagedResultDto<UserGetListOutputDto>>(cacheKey);
            if (cached is not null) return cached;

            var result = await base.GetSelectDataListAsync(keywords);

            var options = result.TotalCount == 0
                ? SfDistributedCacheExtensions.ShortCacheOptions
                : SfDistributedCacheExtensions.DefaultCacheOptions;
            await _distributedCache.SetCacheAsync(cacheKey, result, options);
            return result;
        }

        /// <summary>
        /// 批量查询用户
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<PagedResultDto<UserGetListOutputDto>> GetListAsync(UserGetListInputVo input)
        {
            RefAsync<int> total = 0;
            List<Guid>? deptIds = null;
            if (input.DepartmentId is not null)
            {
                deptIds = await _deptService.GetChildListAsync(input.DepartmentId ?? Guid.Empty);
            }


            List<Guid>? ids = input.Ids?.Split(",").Select(Guid.Parse).ToList();
            List<UserGetListOutputDto> outPut = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.UserName),
                    x => x.UserName!.Contains(input.UserName!))
                .WhereIF(input.Phone is not null, x => x.Phone!.Value.ToString(CultureInfo.InvariantCulture).Contains(input.Phone!.Value.ToString(CultureInfo.InvariantCulture)))
                .WhereIF(!string.IsNullOrEmpty(input.Name), x => x.Name!.Contains(input.Name!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .WhereIF(input.StartTime is not null && input.EndTime is not null,
                    x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)

                //这个为过滤当前部门，加入数据权限后，将由数据权限控制
                .WhereIF(input.DepartmentId is not null, x => deptIds!.Contains(x.DepartmentId ?? Guid.Empty))
                .WhereIF(ids is not null, x => ids!.Contains(x.Id))
                .LeftJoin<Department>((user, dept) => user.DepartmentId == dept.Id)
                .OrderByDescending(user => user.CreationTime)
                .Select((user, dept) => new UserGetListOutputDto(), true)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            List<Guid> userIds = [.. outPut.Select(x => x.Id)];
            if (userIds.Count > 0)
            {
                List<User> usersWithRelations = await _repository._DbQueryable
                    .Includes(u => u.Roles)
                    .Includes(u => u.Posts)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                // O(1) Dictionary 查找替代 O(n) FirstOrDefault
                Dictionary<Guid, User> userDict = usersWithRelations.ToDictionary(u => u.Id);

                foreach (UserGetListOutputDto? dto in outPut)
                {
                    if (userDict.TryGetValue(dto.Id, out User? userEntity))
                    {
                        dto.Roles = ObjectMapper.Map<List<Role>, List<RoleGetListOutputDto>>(userEntity.Roles);
                        dto.Posts = ObjectMapper.Map<List<Position>, List<PostGetListOutputDto>>(userEntity.Posts);
                    }
                }
            }

            PagedResultDto<UserGetListOutputDto> result = new()
            {
                Items = outPut,
                TotalCount = total
            };
            return result;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("添加用户", OperationType.Insert)]
        public override async Task<UserGetOutputDto> CreateAsync(UserCreateInputVo input)
        {
            User entitiy = await MapToEntityAsync(input);

            // R-08: 强制要求管理员在创建用户时设置初始密码
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                throw new UserFriendlyException("创建新用户时，必须显式设置安全的初始密码！", code: "USER_CREATE_001");
            }
            entitiy.SetPassword(input.Password);

            await _userManager.CreateAsync(entitiy);
            await _userManager.GiveUserSetRoleAsync([entitiy.Id], input.RoleIds ?? []);
            await _userManager.GiveUserSetPostAsync([entitiy.Id], input.PostIds ?? []);

            // 同步 Casbin 用户角色关系 (g)
            // 需要获取角色编码，这里假设 RoleIds 对应角色的 Code 需要查询
            // 或者暂时使用 RoleId 作为角色标识（推荐使用 ID 以保持一致性，但 Casbin 通常用 Code 更可读）
            // 方案书V1.2: sub 传递 UserId, g = 用户, 角色, 域

            // 查询角色信息
            // 考虑事务一致性，这里应该在同一个 UOW 中

            // 注意：RoleService/Manager 应该提供获取角色 Code 的方法
            // 这里为了演示，假设 RoleIds 对应的角色列表需要被查询出来
            // 暂时先跳过 Role Code 查询，直接用 RoleId，但最佳实践是 RoleCode
            // 修正：Casbin g策略通常是 g, user_id, role_code, domain_id

            // Casbin 同步已在 GiveUserSetRoleAsync → SetUserRolesAsync 中完成，此处无需重复
            // 已删除 SyncCasbinUserRoles 方法，消除双写 Bug (S-02)

            UserGetOutputDto result = await MapToGetOutputDtoAsync(entitiy);
            InvalidateUserCache();
            return result;
        }

        protected override async Task<User> MapToEntityAsync(UserCreateInputVo createInput)
        {
            // 使用基类的映射逻辑
            User entitiy = await base.MapToEntityAsync(createInput);
            // 注意：此时密码是明文，会在 CreateAsync 中调用 SetPassword 加密
            return entitiy;
        }

        /// <summary>
        /// 单查
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<UserGetOutputDto> GetAsync(Guid id)
        {
            //使用导航树形查询
            User entity = await _repository._DbQueryable.Includes(u => u.Roles).Includes(u => u.Posts)
                .Includes(u => u.Dept).InSingleAsync(id);

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("更新用户", OperationType.Update)]
        public override async Task<UserGetOutputDto> UpdateAsync(Guid id, UserUpdateInputVo input)
        {
            if (input.UserName is UserConst.Admin or UserConst.TenantAdmin)
            {
                throw new UserFriendlyException(UserConst.Name_Not_Allowed);
            }

            if (await _repository.IsAnyAsync(u => input.UserName!.Equals(u.UserName, StringComparison.Ordinal) && !id.Equals(u.Id)))
            {
                throw new UserFriendlyException(UserConst.Exist);
            }

            User entity = await _repository.GetByIdAsync(id);
            //更新密码，特殊处理
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                entity.SetPassword(input.Password);
            }

            await MapToEntityAsync(input, entity);

            await _repository.UpdateAsync(entity);
            await _userManager.GiveUserSetRoleAsync([id], input.RoleIds ?? []);
            await _userManager.GiveUserSetPostAsync([id], input.PostIds ?? []);

            // Casbin 同步已在 GiveUserSetRoleAsync → SetUserRolesAsync 中完成，此处无需重复
            // 已删除 RemoveFilteredGroupingPolicyAsync + SyncCasbinUserRoles 双写 (S-02)

            InvalidateUserCache();
            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 更新个人中心
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("更新个人信息", OperationType.Update)]
        public async Task<UserGetOutputDto> UpdateProfileAsync(ProfileUpdateInputVo input)
        {
            User entity = await _repository.GetByIdAsync(_currentUser.Id.GetValueOrDefault());
            ObjectMapper.Map(input, entity);

            await _repository.UpdateAsync(entity);
            UserGetOutputDto dto = await MapToGetOutputDtoAsync(entity);

            InvalidateUserCache();
            return dto;
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [Route("user/{id}/{state}")]
        [OperLog("更新用户状态", OperationType.Update)]
        public async Task<UserGetOutputDto> UpdateStateAsync([FromRoute] Guid id, [FromRoute] bool state)
        {
            User entity = await _repository.GetByIdAsync(id) ?? throw new UserFriendlyException("用户未存在");
            entity.State = state;
            await _repository.UpdateAsync(entity);
            InvalidateUserCache();
            return await MapToGetOutputDtoAsync(entity);
        }

        [OperLog("删除用户", OperationType.Delete)]
        public override async Task DeleteAsync(Guid id)
        {
            // B-08: 使用 CasbinPolicyManager 统一清理用户策略
            User? user = await _repository.GetByIdAsync(id);
            if (user != null)
            {
                await _casbinPolicyManager.CleanUserPoliciesAsync(user.Id, user.TenantId);
            }

            await base.DeleteAsync(id);
            InvalidateUserCache();
        }

        /// <summary>
        /// 批量删除用户（前端实际调用入口 — 基类单删被禁用远程）
        /// 覆盖基类以追加 Casbin 策略清理 + 缓存失效
        /// </summary>
        [OperLog("批量删除用户", OperationType.Delete)]
        public override async Task DeleteAsync(IEnumerable<Guid> ids)
        {
            // 一次性查询所有待删用户，避免 N+1
            List<User> users = await _repository.GetListAsync(u => ids.Contains(u.Id));
            foreach (User user in users)
            {
                await _casbinPolicyManager.CleanUserPoliciesAsync(user.Id, user.TenantId);
            }

            await base.DeleteAsync(ids);
            InvalidateUserCache();
        }

        /// <summary>
        /// 导出 Excel（优化版本：解决冗余列和集合序列化问题）
        /// </summary>
        public override async Task<IActionResult> GetExportExcelAsync(UserGetListInputVo input)
        {
            // 1. 获取包含关联关系的数据（复用已有的分页查询逻辑，但获取全部数据）
            input.SkipCount = 0;
            input.MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount;
            PagedResultDto<UserGetListOutputDto> listResult = await GetListAsync(input);

            // 2. 将数据映射为专用的导出 DTO，处理“性别”、“角色”、“岗位”等字段的展示格式
            List<UserExportOutputDto> exportData = [.. listResult.Items.Select(x => new UserExportOutputDto
            {
                UserName = x.UserName,
                Name = x.Name,
                Nick = x.Nick,
                Gender = x.Gender switch
                {
                    Gender.Unknown => "未知",
                    Gender.Male => "男",
                    Gender.Female => "女",
                    _ => "未知"
                },
                DeptName = x.DeptName,
                // 关键点：将 List 集合扁平化为逗号分隔的字符串
                RoleNames = x.Roles != null ? string.Join(", ", x.Roles.Select(r => r.RoleName)) : "",
                PostNames = x.Posts != null ? string.Join(", ", x.Posts.Select(p => p.PostName)) : "",
                Phone = x.Phone,
                Email = x.Email,
                State = x.State ? "启用" : "禁用",
                Remark = x.Remark,
                CreationTime = x.CreationTime
            })];

            // 3. 生成 Excel 文件
            string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            string fileName = $"User_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.xlsx";
            string filePath = Path.Combine(tempPath, fileName);

            // MiniExcel 会根据 UserExportOutputDto 上的特性自动处理表头和格式
            await MiniExcel.SaveAsAsync(filePath, exportData);

            // R-11: FileOptions.DeleteOnClose 确保响应结束后自动删除临时文件
            FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        public override Task PostImportExcelAsync(List<UserCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
    }
}
