using Casbin;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;
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
            // 使用 RoleCode 作为策略标识，如果 RoleCode 变更，需要删除旧的策略
            // 这里我们使用更新后的 RoleCode，如果允许修改 RoleCode，需要额外处理

            await _enforcer.RemoveFilteredPolicyAsync(0, entity.RoleCode);
            await SyncCasbinRolePermissions(id, input.MenuIds, entity.RoleCode);

            var dto = await MapToGetOutputDtoAsync(entity);
            return dto;
        }

        private async Task SyncCasbinRolePermissions(Guid roleId, List<Guid> menuIds, string roleCode)
        {
            if (menuIds == null || !menuIds.Any()) return;

            // 获取菜单对应的接口信息
            // 仅处理带有 API 路径的菜单
            var menus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.Id) && !string.IsNullOrEmpty(m.ApiUrl));

            // 方案 V1.2: p = sub, dom, obj, act
            // sub = roleCode (使用 RoleCode 而不是 RoleId，更具可读性)
            // dom = "default" (或从 input 传入，如果支持多域)
            // obj = menu.ApiUrl (RESTful API 路径，如 /api/user/:id)
            // act = menu.ApiMethod (GET, POST 等)

            var policies = new List<string[]>();
            string domain = "default";

            foreach (var menu in menus)
            {
                // 仅当菜单包含有效的 API URL 时
                if (!string.IsNullOrEmpty(menu.ApiUrl))
                {
                    string path = menu.ApiUrl;
                    // 使用 Menu 实体的 ApiMethod 字段，如果为空则默认为 GET
                    string method = !string.IsNullOrEmpty(menu.ApiMethod) ? menu.ApiMethod.ToUpper() : "GET";

                    policies.Add(new[] { roleCode, domain, path, method });
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

            // 查询 RoleCode
            var role = await _repository.GetByIdAsync(input.RoleId);
            var roleCode = role?.RoleCode ?? throw new UserFriendlyException("角色不存在");

            var policies = input.UserIds.Select(userId => new[] { userId.ToString(), roleCode, domain }).ToList();
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

            // 查询 RoleCode
            var role = await _repository.GetByIdAsync(input.RoleId);
            var roleCode = role?.RoleCode ?? throw new UserFriendlyException("角色不存在");

            var policies = input.UserIds.Select(userId => new[] { userId.ToString(), roleCode, domain }).ToList();
            await _enforcer.RemoveGroupingPoliciesAsync(policies);
            await _enforcer.SavePolicyAsync();
        }
    }
}