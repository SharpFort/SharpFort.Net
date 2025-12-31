using Volo.Abp.Domain.Services;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    public class RoleManager : DomainService
    {
        private ISqlSugarRepository<Role> _repository;
        private ISqlSugarRepository<RoleMenu> _roleMenuRepository;
        private ISqlSugarRepository<Menu> _menuRepository;
        private ICasbinPolicyManager _casbinPolicyManager;

        public RoleManager(
            ISqlSugarRepository<Role> repository, 
            ISqlSugarRepository<RoleMenu> roleMenuRepository,
            ISqlSugarRepository<Menu> menuRepository,
            ICasbinPolicyManager casbinPolicyManager)
        {
            _repository = repository;
            _roleMenuRepository = roleMenuRepository;
            _menuRepository = menuRepository;
            _casbinPolicyManager = casbinPolicyManager;
        }

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
            foreach (var roleId in roleIds)
            {
                // 添加新的关系
                List<RoleMenu> roleMenus = new();
                foreach (var menu in menuIds)
                {
                    roleMenus.Add(new RoleMenu() { RoleId = roleId, MenuId = menu });
                }
                // 一次性批量添加
                await _roleMenuRepository.InsertRangeAsync(roleMenus);
            }

            // 2. Casbin 策略同步
            // 获取所有涉及的角色实体和菜单实体
            var roles = await _repository.GetListAsync(r => roleIds.Contains(r.Id));
            // 获取选中的菜单实体（包含 ApiUrl）
            var menus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.Id));

            foreach (var role in roles)
            {
                // 同步该角色的所有 API 权限
                await _casbinPolicyManager.SetRolePermissionsAsync(role, menus);
            }
        }
    }
}
