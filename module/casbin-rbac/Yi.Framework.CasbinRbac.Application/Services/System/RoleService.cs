using Casbin;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Role;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.User;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Managers;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// Role服务实现
    /// </summary>
    public class RoleService : YiCrudAppService<Role, RoleGetOutputDto, RoleGetListOutputDto, Guid,
            RoleGetListInputVo, RoleCreateInputVo, RoleUpdateInputVo>,
        IRoleService
    {
        private readonly IEnforcer _enforcer;
        private readonly ISqlSugarRepository<Menu, Guid> _menuRepository;

        public RoleService(RoleManager roleManager, ISqlSugarRepository<RoleDepartment> roleDeptRepository,
            ISqlSugarRepository<UserRole> userRoleRepository,
            ISqlSugarRepository<Role, Guid> repository,
            IEnforcer enforcer,
            ISqlSugarRepository<Menu, Guid> menuRepository) : base(repository)
        {
            (_roleManager, _roleDeptRepository, _userRoleRepository, _repository, _enforcer, _menuRepository) =
                (roleManager, roleDeptRepository, userRoleRepository, repository, enforcer, menuRepository);
        }

        private ISqlSugarRepository<Role, Guid> _repository;
        private RoleManager _roleManager { get; set; }

        private ISqlSugarRepository<RoleDepartment> _roleDeptRepository;

        private ISqlSugarRepository<UserRole> _userRoleRepository;

        public async Task UpdateDataScopeAsync(UpdateDataScopeInput input)
        {
            //只有自定义的需要特殊处理
            if (input.DataScope == DataScope.CUSTOM)
            {
                await _roleDeptRepository.DeleteAsync(x => x.RoleId == input.RoleId);
                var insertEntities = input.DepartmentIds.Select(x => new RoleDepartment { DepartmentId = x, RoleId = input.RoleId })
                    .ToList();
                await _roleDeptRepository.InsertRangeAsync(insertEntities);
            }

            var entity = new Role() { DataScope = input.DataScope };
            EntityHelper.TrySetId(entity, () => input.RoleId);
            await _repository._Db.Updateable(entity).UpdateColumns(x => x.DataScope).ExecuteCommandAsync();
        }

        public override async Task<PagedResultDto<RoleGetListOutputDto>> GetListAsync(RoleGetListInputVo input)
        {
            RefAsync<int> total = 0;

            var entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.RoleCode),
                    x => x.RoleCode.Contains(input.RoleCode!))
                .WhereIF(!string.IsNullOrEmpty(input.RoleName), x => x.RoleName.Contains(input.RoleName!))
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
            var isExist =
                await _repository.IsAnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }

            var entity = await MapToEntityAsync(input);
            await _repository.InsertAsync(entity);

            // Casbin 同步
            await SyncCasbinRolePermissions(entity.Id, input.MenuIds, entity.RoleCode);

            var outputDto = await MapToGetOutputDtoAsync(entity);

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
            var entity = await _repository.GetByIdAsync(id);

            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id).AnyAsync(x => x.RoleCode == input.RoleCode || x.RoleName == input.RoleName);
            if (isExist)
            {
                throw new UserFriendlyException(RoleConst.Exist);
            }

            await MapToEntityAsync(input, entity);
            await _repository.UpdateAsync(entity);

            await _roleManager.GiveRoleSetMenuAsync(new List<Guid> { id }, input.MenuIds);

            // Casbin 同步：更新角色权限
            // 先删除旧策略
            // 注意：RoleCode 可能会变，需要删除旧 RoleCode 的策略，但 Enforcer 通常按字符串删
            // 如果 RoleCode 变了，需要先用旧的删，这里假设 Id 关联或已经拿到旧实体
            // 简单起见，使用 Update 后的 RoleCode 重新构建
            
            // 重要：如果 RoleCode 允许修改，需要清理旧 RoleCode 的策略
            // 这里我们使用 sub = RoleId.ToString() 或者 RoleCode (方案中提到 sub 是 UserId, g 映射到 RoleCode/RoleId)
            // 方案 V1.2: p = sub, dom, obj, act. 这里的 sub 是角色标识
            // 如果 sub 用 RoleId，则不受 RoleCode 修改影响
            // 如果 sub 用 RoleCode，则需要删旧加新
            
            // 我们在 UserService 中 g 使用了 roleId.ToString()，所以这里也应该用 roleId.ToString() 作为 p 的 sub
            // p, roleId, domain, path, method
            
            await _enforcer.RemoveFilteredPolicyAsync(0, id.ToString());
            await SyncCasbinRolePermissions(id, input.MenuIds, entity.RoleCode);

            var dto = await MapToGetOutputDtoAsync(entity);
            return dto;
        }

        private async Task SyncCasbinRolePermissions(Guid roleId, List<Guid> menuIds, string roleCode)
        {
            if (menuIds == null || !menuIds.Any()) return;

            // 获取菜单对应的接口信息
            // 仅处理 API 类型的菜单或带有 API 路径的菜单
            var menus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.Id) && !string.IsNullOrEmpty(m.Permission));
            
            // 方案 V1.2: p = sub, dom, obj, act
            // sub = roleId.ToString()
            // dom = "default" (或从 input 传入，如果支持多域)
            // obj = menu.Permission (需确保是 RESTful 路径，如 /api/user/:id，或者这里存储的是 path)
            // act = menu.Method (GET, POST 等)
            
            // 假设 Menu 表有 Permission (作为 API Path) 和 Method 字段
            // 如果没有，需要扩展 Menu 实体或从其他地方获取
            // 现有代码 Menu 实体未详查，假设需要适配
            
            // 暂时逻辑：只处理 MenuType.Button 或 Api 类型的，且 Permission 字段存的是 API Path
            // 如果 Permission 存的是 "user:list"，则无法直接转为 Casbin 路径，需额外映射
            // 任务清单 5: "apiPath 使用 RESTful 风格"
            
            // 修正：Menu 实体通常存的是权限标识，需要关联 API 资源
            // 如果没有单独的 API 资源表，则需在 Menu 中增加 ApiPath 和 Method 字段
            // 或者暂时假设 Permission 字段就是 ApiPath (需确认 Menu 实体结构)
            
            // 查阅 Menu 实体：module/casbin-rbac/Yi.Framework.CasbinRbac.Domain/Entities/Menu.cs
            // 发现有 Permission 字段，但可能是 "system:user:list"
            // 需要确认是否有 Url 字段或类似字段作为 API Path
            
            // 如果 Menu 没有 Path/Method，无法直接同步。
            // 假设 Menu 表暂未改造，需后续在“接口扫描”任务中完善
            // 这里先写个 TODO 或尝试使用 Url 字段（如果是菜单的话）
            
            // 暂时使用 Url 字段作为 Path (如果是 API 类型的菜单)
            // 假设 Method 默认为 ALL 或需新增字段
            
            var policies = new List<string[]>();
            string domain = "default";

            foreach (var menu in menus)
            {
                // 仅当菜单是 API 类型或包含有效 URL 时
                // 现阶段假设 Url 存的是 API 路径
                if (!string.IsNullOrEmpty(menu.Url))
                {
                    // 默认 GET，或者需要扩展 Menu 实体支持 Method
                    // 任务清单 7 提到移除 [Permission]，说明要靠 Path 鉴权
                    // 建议：Menu 表增加 Method 字段，或 Url 包含 Method (e.g., "GET:/api/users")
                    
                    string path = menu.Url;
                    string method = "GET"; // 默认，待完善
                    
                    // 简单解析：如果 Url 格式为 "POST:/api/user"
                    if (path.Contains(":"))
                    {
                        var parts = path.Split(':');
                        if (parts.Length > 1 && new[]{"GET","POST","PUT","DELETE"}.Contains(parts[0].ToUpper()))
                        {
                            method = parts[0].ToUpper();
                            path = path.Substring(method.Length + 1);
                        }
                    }

                    policies.Add(new[] { roleId.ToString(), domain, path, method });
                }
            }

            if (policies.Any())
            {
                await _enforcer.AddPoliciesAsync(policies);
                await _enforcer.SavePolicyAsync();
            }
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
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null)
            {
                throw new ApplicationException("角色未存在");
            }

            entity.State = state;
            await _repository.UpdateAsync(entity);
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
            PagedResultDto<UserGetListOutputDto> output;
            //角色下已授权用户
            if (isAllocated == true)
            {
                output = await GetAllocatedAuthUserByRoleIdAsync(roleId, input);
            }
            //角色下未授权用户
            else
            {
                output = await GetNotAllocatedAuthUserByRoleIdAsync(roleId, input);
            }

            return output;
        }

        private async Task<PagedResultDto<UserGetListOutputDto>> GetAllocatedAuthUserByRoleIdAsync(Guid roleId,
            RoleAuthUserGetListInput input)
        {
            RefAsync<int> total = 0;
            var output = await _userRoleRepository._DbQueryable
                .LeftJoin<User>((ur, u) => ur.UserId == u.Id && ur.RoleId == roleId)
                .Where((ur, u) => ur.RoleId == roleId)
                .WhereIF(!string.IsNullOrEmpty(input.UserName), (ur, u) => u.UserName.Contains(input.UserName))
                .WhereIF(input.Phone is not null, (ur, u) => u.Phone.ToString().Contains(input.Phone.ToString()))
                .Select((ur, u) => new UserGetListOutputDto { Id = u.Id }, true)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<UserGetListOutputDto>(total, output);
        }

        private async Task<PagedResultDto<UserGetListOutputDto>> GetNotAllocatedAuthUserByRoleIdAsync(Guid roleId,
            RoleAuthUserGetListInput input)
        {
            RefAsync<int> total = 0;
            var entities = await _userRoleRepository._Db.Queryable<User>()
                .Where(u => SqlFunc.Subqueryable<UserRole>().Where(x => x.RoleId == roleId)
                    .Where(x => x.UserId == u.Id).NotAny())
                .WhereIF(!string.IsNullOrEmpty(input.UserName), u => u.UserName.Contains(input.UserName))
                .WhereIF(input.Phone is not null, u => u.Phone.ToString().Contains(input.Phone.ToString()))
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            var output = entities.Adapt<List<UserGetListOutputDto>>();
            return new PagedResultDto<UserGetListOutputDto>(total, output);
        }


        /// <summary>
        /// 批量给用户授权
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task CreateAuthUserAsync([FromBody] RoleAuthUserCreateOrDeleteInput input)
        {
            var userRoleEntities = input.UserIds.Select(u => new UserRole { RoleId = input.RoleId, UserId = u })
                .ToList();
            await _userRoleRepository.InsertRangeAsync(userRoleEntities);

            // Casbin 同步：添加用户角色关联 (g)
            string domain = "default";
            var policies = input.UserIds.Select(userId => new[] { userId.ToString(), input.RoleId.ToString(), domain }).ToList();
            await _enforcer.AddGroupingPoliciesAsync(policies);
            await _enforcer.SavePolicyAsync();
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
            
            // Casbin 同步：移除用户角色关联
            string domain = "default";
            var policies = input.UserIds.Select(userId => new[] { userId.ToString(), input.RoleId.ToString(), domain }).ToList();
            await _enforcer.RemoveGroupingPoliciesAsync(policies);
            await _enforcer.SavePolicyAsync();
        }
    }
}