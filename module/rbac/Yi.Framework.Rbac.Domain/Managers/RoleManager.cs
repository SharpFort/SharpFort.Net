using Volo.Abp.Domain.Services;
using Yi.Framework.Rbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Rbac.Domain.Managers
{
    public class RoleManager : DomainService
    {
        private ISqlSugarRepository<Role> _repository;
        private ISqlSugarRepository<RoleMenu> _roleMenuRepository;
        public RoleManager(ISqlSugarRepository<Role> repository, ISqlSugarRepository<RoleMenu> roleMenuRepository)
        {
            _repository = repository;
            _roleMenuRepository = roleMenuRepository;
        }

        /// <summary>
        /// 给角色设置菜单
        /// </summary>
        /// <param name="roleIds"></param>
        /// <param name="menuIds"></param>
        /// <returns></returns>
        public async Task GiveRoleSetMenuAsync(List<Guid> roleIds, List<Guid> menuIds)
        {
            //这个是需要事务的，在service中进行工作单元
            await _roleMenuRepository.DeleteAsync(u => roleIds.Contains(u.RoleId));
            //遍历用户
            foreach (var roleId in roleIds)
            {
                //添加新的关系
                List<RoleMenu> RoleMenu = new();
                foreach (var menu in menuIds)
                {
                    RoleMenu.Add(new RoleMenu() { RoleId = roleId, MenuId = menu });
                }
                //一次性批量添加
                await _roleMenuRepository.InsertRangeAsync(RoleMenu);
            }

        }
    }
}
