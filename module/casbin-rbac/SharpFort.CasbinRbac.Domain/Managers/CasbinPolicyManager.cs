using Casbin;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;
using Casbin.Adapter.SqlSugar.Entities;



namespace SharpFort.CasbinRbac.Domain.Managers
{
    public class CasbinPolicyManager(
        IEnforcer enforcer,
        IUnitOfWorkManager unitOfWorkManager,
        ISqlSugarRepository<Role> roleRepository,
        ICurrentTenant currentTenant) : DomainService, ICasbinPolicyManager
    {
        private readonly IEnforcer _enforcer = enforcer;
        private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;
        private readonly ISqlSugarRepository<Role> _roleRepository = roleRepository;
        private readonly ICurrentTenant _currentTenant = currentTenant;

        #region Helper Methods

        private static string GetUserSubject(Guid userId)
        {
            return userId.ToString();
        }

        private static string GetRoleSubject(string roleCode)
        {
            return roleCode;
        }

        private string GetTenantDomain(Guid? tenantId)
        {
            Guid? finalTenantId = tenantId ?? _currentTenant.Id;
            return finalTenantId?.ToString() ?? "default";
        }

        #endregion

        /// <summary>
        /// 内存同步核心方法
        /// 仅在事务成功提交后触发全量重载 (保证最终一致性)
        /// 使用工作单元Items字典实现防抖，确保一个事务内只注册一次回调
        /// </summary>
        private void TriggerMemorySync()
        {
            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null)
            {
                const string syncKey = "CasbinMemorySyncTriggered";
                if (!uow.Items.ContainsKey(syncKey))
                {
                    uow.Items[syncKey] = true;
                    uow.OnCompleted(async () =>
                    {
                        await _enforcer.LoadPolicyAsync();
                    });
                }
            }
            else
            {
                _enforcer.LoadPolicy();
            }
        }

        public async Task AddRoleForUserAsync(User user, Role role)
        {
            string sub = GetUserSubject(user.Id);
            string roleSub = GetRoleSubject(role.RoleCode);
            string domain = GetTenantDomain(user.TenantId);

            // 1. 持久化
            // g rule: V0=sub(User), V1=role(Role), V2=domain
            CasbinRule rule = new()
            {
                PType = "g",
                V0 = sub,
                V1 = roleSub,
                V2 = domain
            };
            await _roleRepository._Db.Insertable(rule).ExecuteCommandAsync();

            // 2. 内存更新
            await _enforcer.AddGroupingPolicyAsync(sub, roleSub, domain);

            TriggerMemorySync();
        }

        public async Task RemoveRoleForUserAsync(User user, Role role)
        {
            string sub = GetUserSubject(user.Id);
            string roleSub = GetRoleSubject(role.RoleCode);
            string domain = GetTenantDomain(user.TenantId);

            // 1. 持久化
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain).ExecuteCommandAsync();

            // 2. 内存更新
            await _enforcer.RemoveGroupingPolicyAsync(sub, roleSub, domain);

            TriggerMemorySync();
        }

        public async Task SetUserRolesAsync(User user, List<Role> roles)
        {
            string sub = GetUserSubject(user.Id);
            string domain = GetTenantDomain(user.TenantId);

            // 1. 持久化
            // 先物理删除该用户在该租户下的所有角色关联 (g, sub, ?, domain)
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain).ExecuteCommandAsync();

            // 批量插入新关联
            if (roles.Count > 0)
            {
                List<CasbinRule> newRules = [.. roles.Select(r => new CasbinRule
                {
                    PType = "g",
                    V0 = sub,
                    V1 = GetRoleSubject(r.RoleCode),
                    V2 = domain
                })];
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }

            // 2. 内存更新
            // 先清理旧数据 (Standard way: load user roles first then remove)
            IEnumerable<string> oldRoles = _enforcer.GetRolesForUserInDomain(sub, domain);
            foreach (string r in oldRoles)
            {
                await _enforcer.RemoveGroupingPolicyAsync(sub, r, domain);
            }

            // 插入新数据
            if (roles.Count > 0)
            {
                List<string[]> policies = [.. roles.Select(r => new[] {
                    sub,
                    GetRoleSubject(r.RoleCode),
                    domain
                })];

                await _enforcer.AddGroupingPoliciesAsync(policies);
            }

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
        {
            string roleSub = GetRoleSubject(role.RoleCode);
            string domain = GetTenantDomain(role.TenantId);

            // 1. 持久化
            // 删除该角色在该域下的所有权限 (p, roleSub, domain, ?, ?)
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain).ExecuteCommandAsync();

            List<string[]> newPolicies = [];
            List<CasbinRule> newRules = [];

            foreach (Menu menu in menus)
            {
                if (string.IsNullOrWhiteSpace(menu.ApiUrl))
                {
                    continue;
                }

                string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;

                // 内存对象
                newPolicies.Add([
                    roleSub,
                    domain,
                    menu.ApiUrl,
                    methods
                ]);

                // 数据库对象
                newRules.Add(new CasbinRule
                {
                    PType = "p",
                    V0 = roleSub,
                    V1 = domain,
                    V2 = menu.ApiUrl,
                    V3 = methods
                });
            }

            // 批量插入
            if (newRules.Count > 0)
            {
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }

            // 2. 内存更新
            // 先清理 (Memory way)
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);

            // 插入新数据
            if (newPolicies.Count > 0)
            {
                await _enforcer.AddPoliciesAsync(newPolicies);
            }

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task InitAdminPermissionAsync(Role adminRole)
        {
            string roleSub = GetRoleSubject(adminRole.RoleCode);
            string domain = GetTenantDomain(adminRole.TenantId);

            // 1. 持久化
            // 清理
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain).ExecuteCommandAsync();

            // 插入无敌规则
            await _roleRepository._Db.Insertable(new CasbinRule
            {
                PType = "p",
                V0 = roleSub,
                V1 = domain,
                V2 = "*",
                V3 = "*"
            }).ExecuteCommandAsync();

            // 2. 内存更新
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            await _enforcer.AddPolicyAsync(roleSub, domain, "*", "*");

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task CleanRolePoliciesAsync(Role role)
        {
            string roleSub = GetRoleSubject(role.RoleCode);
            string domain = GetTenantDomain(role.TenantId);

            // 1. 持久化
            // 清理该角色的 p 规则 (权限) 和 g 规则 (作为角色与用户的绑定)
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain) || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain)).ExecuteCommandAsync();

            // 2. 内存更新
            // 移除该角色下所有的 p 规则
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            // 移除所有带有该角色的 g 规则 (用户绑定)
            // _enforcer.RemoveGroupingPolicyAsync is singular, we need to remove by filter
            await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task CleanRolePoliciesByRoleCodeAsync(string roleCode, Guid? tenantId)
        {
            string roleSub = GetRoleSubject(roleCode);
            string domain = GetTenantDomain(tenantId);

            // 1. 持久化
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain) || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain)).ExecuteCommandAsync();

            // 2. 内存更新
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
            await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);

            // 3. 触发同步
            TriggerMemorySync();
        }
    }
}
