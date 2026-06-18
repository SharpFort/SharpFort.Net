using Volo.Abp.Domain.Services;
using Microsoft.Extensions.Options;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    public class RoleManager(
        ISqlSugarRepository<Role> repository,
        ISqlSugarRepository<RoleMenu> roleMenuRepository,
        ISqlSugarRepository<Menu> menuRepository,
        ICasbinPolicyManager casbinPolicyManager,
        IOptions<CasbinOptions> casbinOptions) : DomainService
    {
        private readonly ISqlSugarRepository<Role> _repository = repository;
        private readonly ISqlSugarRepository<RoleMenu> _roleMenuRepository = roleMenuRepository;
        private readonly ISqlSugarRepository<Menu> _menuRepository = menuRepository;
        private readonly ICasbinPolicyManager _casbinPolicyManager = casbinPolicyManager;
        private readonly string _adminRoleCode = casbinOptions.Value.SuperAdminRoleCode ?? UserConst.AdminRolesCode;

        /// <summary>
        /// 给角色设置菜单
        /// </summary>
        /// <param name="roleIds"></param>
        /// <param name="menuIds"></param>
        /// <returns></returns>
        public async Task GiveRoleSetMenuAsync(List<Guid> roleIds, List<Guid> menuIds)
        {
            // 1. 业务数据持久化
            // 这个是需要事务的，在service中进行工作单元
            await _roleMenuRepository.DeleteAsync(u => roleIds.Contains(u.RoleId));

            // 遍历角色
            foreach (Guid roleId in roleIds)
            {
                // 添加新的关系
                List<RoleMenu> roleMenus = [];
                foreach (Guid menu in menuIds)
                {
                    roleMenus.Add(new RoleMenu() { RoleId = roleId, MenuId = menu });
                }
                // 一次性批量添加
                await _roleMenuRepository.InsertRangeAsync(roleMenus);
            }

            // 2. Casbin 策略同步
            // 获取所有涉及的角色实体和菜单实体
            // F-05: 纵深防御 — 排除超管角色（超管由 *,* 覆盖）
            List<Role> roles = await _repository.GetListAsync(
                r => roleIds.Contains(r.Id) && r.RoleCode != _adminRoleCode);
            // 获取选中的菜单实体（包含 ApiUrl）
            List<Menu> menus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.Id));

            foreach (Role role in roles)
            {
                // 同步该角色的所有 API 权限
                await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
            }
        }
    }
}
