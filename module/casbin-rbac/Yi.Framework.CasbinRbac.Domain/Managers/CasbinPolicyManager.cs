using Casbin;
using Volo.Abp.Domain.Services;
using Yi.Framework.CasbinRbac.Domain.Entities;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    public class CasbinPolicyManager : DomainService, ICasbinPolicyManager
    {
        private readonly IEnforcer _enforcer;

        public CasbinPolicyManager(IEnforcer enforcer)
        {
            _enforcer = enforcer;
        }

        #region Helper Methods

        private string GetUserSubject(Guid userId) => $"u_{userId}";
        private string GetRoleSubject(string roleCode) => roleCode; // 角色直接用 Code
        private string GetTenantDomain(Guid? tenantId) => tenantId?.ToString() ?? "default";

        #endregion

        /// <summary>
        /// 给用户分配角色 (g policy)
        /// 规则: g, u_UserId, RoleCode, TenantId
        /// </summary>
        public async Task AddRoleForUserAsync(User user, Role role)
        {
            var sub = GetUserSubject(user.Id);
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(user.TenantId);

            await _enforcer.AddGroupingPolicyAsync(sub, roleSub, domain);
        }

        /// <summary>
        /// 移除用户的角色
        /// </summary>
        public async Task RemoveRoleForUserAsync(User user, Role role)
        {
            var sub = GetUserSubject(user.Id);
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(user.TenantId);

            await _enforcer.RemoveGroupingPolicyAsync(sub, roleSub, domain);
        }

        /// <summary>
        /// 设置用户的角色列表 (全量覆盖)
        /// 先清空该用户在该租户下的所有角色关联，再重新添加
        /// </summary>
        public async Task SetUserRolesAsync(User user, List<Role> roles)
        {
            var sub = GetUserSubject(user.Id);
            var domain = GetTenantDomain(user.TenantId);

            // 1. 移除旧策略: g, sub, ?, domain
            // RemoveFilteredGroupingPolicyAsync(fieldIndex, fieldValues...)
            // g 策略结构: sub, role, domain
            // 我们要匹配 fieldIndex=0 (sub) 和 fieldIndex=2 (domain)
            // Casbin 的 RemoveFilteredPolicy 通常是连续匹配。
            // g 的定义在 conf 中是 _, _, _ (User, Role, Domain)
            // RemoveFilteredGroupingPolicy(0, sub, "", domain) -> 这样可能不行，中间空字符串会被当做匹配条件吗？
            // 通常 Casbin 的 RemoveFilteredPolicy API: (fieldIndex, fieldValues...)
            // 如果我们要跳过中间的 Role，可能比较麻烦。
            
            // 策略 A: 查出当前用户所有的角色，然后逐个删除。
            // 策略 B: 使用 Batch API。
            
            // 这里我们先尝试查出该用户的所有 g 策略
            var oldPolicies = _enforcer.GetFilteredGroupingPolicy(0, sub); 
            // 注意：GetFilteredGroupingPolicy 仅过滤 fieldIndex 0，我们需要自己过滤 domain
            var toRemove = oldPolicies.Where(p => p.Count > 2 && p[2] == domain).ToList();
            
            if (toRemove.Any())
            {
                await _enforcer.RemoveGroupingPoliciesAsync(toRemove);
            }

            // 2. 添加新策略
            if (roles.Any())
            {
                var newPolicies = roles.Select(r => new List<string> 
                { 
                    sub, 
                    GetRoleSubject(r.RoleCode), 
                    domain 
                }).ToList(); // 这里不用 IReadOnlyList<string>，List<string> 也是兼容的

                await _enforcer.AddGroupingPoliciesAsync(newPolicies);
            }
        }

        /// <summary>
        /// 设置角色的权限 (p policy)
        /// 规则: p, RoleCode, TenantId, ApiUrl, Method
        /// </summary>
        public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
        {
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(role.TenantId);

            // 1. 清空该角色在该租户下的所有权限
            // p 策略结构: sub, dom, obj, act
            // RemoveFilteredPolicyAsync(0, roleSub, domain) 
            // fieldIndex=0 -> sub(role), fieldIndex=1 -> dom(tenant)
            // 这正好匹配 p 的定义的前两个字段，完美。
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);

            // 2. 构建新策略
            var policies = new List<IList<string>>();
            foreach (var menu in menus)
            {
                if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;

                var methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
                // 支持逗号分隔的方法? 比如 "GET,POST"
                // Casbin 正则匹配通常写成 "(GET)|(POST)"
                // 这里简单处理，假设 ApiMethod 就是标准的单个方法或通配符，或者已经格式化好的正则
                
                policies.Add(new List<string>
                {
                    roleSub,
                    domain,
                    menu.ApiUrl,
                    methods
                });
            }

            if (policies.Any())
            {
                await _enforcer.AddPoliciesAsync(policies);
            }
        }

        /// <summary>
        /// 初始化超级管理员权限
        /// </summary>
        public async Task InitAdminPermissionAsync(Role adminRole)
        {
            var roleSub = GetRoleSubject(adminRole.RoleCode);
            var domain = GetTenantDomain(adminRole.TenantId);

            // 先清理
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);

            // 添加通配符策略: p, admin, domain, *, *
            await _enforcer.AddPolicyAsync(roleSub, domain, "*", "*");
        }
    }
}
