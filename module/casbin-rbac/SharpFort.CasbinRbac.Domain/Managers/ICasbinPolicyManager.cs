using Volo.Abp.Domain.Services;
using SharpFort.CasbinRbac.Domain.Entities;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    public interface ICasbinPolicyManager : IDomainService
    {
        /// <summary>
        /// 给用户分配角色 (g policy)
        /// g, u_{userId}, {roleCode}, {tenantId}
        /// </summary>
        Task AddRoleForUserAsync(User user, Role role);

        /// <summary>
        /// 移除用户的角色 (g policy)
        /// </summary>
        Task RemoveRoleForUserAsync(User user, Role role);

        /// <summary>
        /// 设置用户的角色列表 (全量覆盖)
        /// </summary>
        Task SetUserRolesAsync(User user, List<Role> roles);

        /// <summary>
        /// 设置角色的权限 (p policy)
        /// 根据菜单配置的 API 自动生成策略
        /// </summary>
        Task SetRolePermissionsAsync(Role role, List<Menu> menus);

        /// <summary>
        /// 初始化/重置超级管理员权限 (通配符 *)
        /// </summary>
        Task InitAdminPermissionAsync(Role adminRole);

        /// <summary>
        /// 清理角色所有的策略 (p policy 和 g policy)
        /// 用于删除角色时
        /// </summary>
        Task CleanRolePoliciesAsync(Role role);

        /// <summary>
        /// 根据角色编码清理所有策略 (p policy 和 g policy)
        /// 用于角色编码变更时清理旧编码的策略
        /// </summary>
        Task CleanRolePoliciesByRoleCodeAsync(string roleCode, Guid? tenantId);

        /// <summary>
        /// 当角色编码变更时，在底层迁移所有关联的 p 规则和 g 规则，避免用户角色关联丢失 (R-05)
        /// </summary>
        Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId);

        /// <summary>
        /// 清理用户所有的 Casbin 策略（删除用户时调用）(B-08)
        /// </summary>
        Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId);

        /// <summary>
        /// 带全局写锁的全量策略重载（供系统初始化、运维手动触发及数据迁移使用）。
        /// </summary>
        Task ReloadAllPoliciesAsync();
    }
}
