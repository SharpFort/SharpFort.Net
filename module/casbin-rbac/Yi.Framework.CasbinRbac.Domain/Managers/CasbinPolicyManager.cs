using Casbin;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;
using Casbin.Adapter.SqlSugar.Entities;
 


namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    public class CasbinPolicyManager : DomainService, ICasbinPolicyManager
    {
        private readonly IEnforcer _enforcer;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ISqlSugarRepository<Role> _roleRepository;

        public CasbinPolicyManager(
            IEnforcer enforcer, 
            IUnitOfWorkManager unitOfWorkManager,
            ISqlSugarRepository<Role> roleRepository)
        {
            _enforcer = enforcer;
            _unitOfWorkManager = unitOfWorkManager;
            _roleRepository = roleRepository;
        }

        #region Helper Methods

        private string GetUserSubject(Guid userId) => $"u_{userId}";
        private string GetRoleSubject(string roleCode) => roleCode;
        private string GetTenantDomain(Guid? tenantId) => tenantId?.ToString() ?? "default";

        #endregion

        /// <summary>
        /// 内存同步核心方法
        /// 仅在事务成功提交后触发全量重载 (保证最终一致性)
        /// </summary>
        private void TriggerMemorySync()
        {
            // 如果存在当前工作单元，则注册回调
            if (_unitOfWorkManager.Current != null)
            {
                _unitOfWorkManager.Current.OnCompleted(async () =>
                {
                    // 事务已提交，DB 中是最新的，此时加载最安全
                    // 注意：LoadPolicyAsync 内部有锁，高并发下可能会有锁竞争，但在管理端操作频率下可接受
                    await _enforcer.LoadPolicyAsync();
                });
            }
            else
            {
                // 如果没有事务（罕见），则立即加载
                _enforcer.LoadPolicy();
            }
        }

        public async Task AddRoleForUserAsync(User user, Role role)
        {
            var sub = GetUserSubject(user.Id);
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(user.TenantId);
            
            // 1. 持久化
            // g rule: V0=sub(User), V1=role(Role), V2=domain
            var rule = new CasbinRule
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
            var sub = GetUserSubject(user.Id);
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(user.TenantId);

            // 1. 持久化
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain).ExecuteCommandAsync();

            // 2. 内存更新
            await _enforcer.RemoveGroupingPolicyAsync(sub, roleSub, domain);

            TriggerMemorySync();
        }

        public async Task SetUserRolesAsync(User user, List<Role> roles)
        {
            var sub = GetUserSubject(user.Id);
            var domain = GetTenantDomain(user.TenantId);

            // 1. 持久化
            // 先物理删除该用户在该租户下的所有角色关联 (g, sub, ?, domain)
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain).ExecuteCommandAsync();
            
            // 批量插入新关联
            if (roles.Any())
            {
                var newRules = roles.Select(r => new CasbinRule
                {
                    PType = "g",
                    V0 = sub,
                    V1 = GetRoleSubject(r.RoleCode),
                    V2 = domain
                }).ToList();
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }

            // 2. 内存更新
            // 先清理旧数据 (Standard way: load user roles first then remove)
            var oldRoles = _enforcer.GetRolesForUserInDomain(sub, domain);
            foreach (var r in oldRoles)
            {
                await _enforcer.RemoveGroupingPolicyAsync(sub, r, domain);
            }

            // 插入新数据
            if (roles.Any())
            {
                var policies = roles.Select(r => new[] { 
                    sub, 
                    GetRoleSubject(r.RoleCode), 
                    domain 
                }).ToList();

                await _enforcer.AddGroupingPoliciesAsync(policies);
            }

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
        {
            var roleSub = GetRoleSubject(role.RoleCode);
            var domain = GetTenantDomain(role.TenantId);

            // 1. 持久化
            // 删除该角色在该域下的所有权限 (p, roleSub, domain, ?, ?)
            await _roleRepository._Db.Deleteable<CasbinRule>().Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain).ExecuteCommandAsync();

            var newPolicies = new List<string[]>();
            var newRules = new List<CasbinRule>();

            foreach (var menu in menus)
            {
                if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;

                var methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;

                // 内存对象
                newPolicies.Add(new[] { 
                    roleSub, 
                    domain, 
                    menu.ApiUrl, 
                    methods 
                });

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
            if (newRules.Any())
            {
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }

            // 2. 内存更新
            // 先清理 (Memory way)
            await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
 
            // 插入新数据
            if (newPolicies.Any())
            {
                await _enforcer.AddPoliciesAsync(newPolicies);
            }

            // 3. 触发同步
            TriggerMemorySync();
        }

        public async Task InitAdminPermissionAsync(Role adminRole)
        {
            var roleSub = GetRoleSubject(adminRole.RoleCode);
            var domain = GetTenantDomain(adminRole.TenantId);

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
    }
}

