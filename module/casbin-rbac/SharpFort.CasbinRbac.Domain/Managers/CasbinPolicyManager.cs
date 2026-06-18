using Casbin;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Volo.Abp.MultiTenancy;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore.Abstractions;
using Casbin.Adapter.SqlSugar.Entities;


namespace SharpFort.CasbinRbac.Domain.Managers
{
    public class CasbinPolicyManager(
        IEnforcer enforcer,
        IUnitOfWorkManager unitOfWorkManager,
        ISqlSugarRepository<Role> roleRepository,
        ICurrentTenant currentTenant,
        IOptions<CasbinOptions> casbinOptions) : DomainService, ICasbinPolicyManager
    {
        private readonly IEnforcer _enforcer = enforcer;
        private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;
        private readonly ISqlSugarRepository<Role> _roleRepository = roleRepository;
        private readonly ICurrentTenant _currentTenant = currentTenant;
        private readonly string _adminRoleCode = casbinOptions.Value.SuperAdminRoleCode ?? UserConst.AdminRolesCode;

        // 全局写操作互斥锁，消除并发写 Enforcer 内存竞态 (R-02)
        private static readonly SemaphoreSlim _writeLock = new(1, 1);

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
        /// 无 UOW 时的一致性兜底：先尝试增量同步，失败则全量重载。
        /// </summary>
        private async Task SyncOrFallback(Func<Task> incrementalSync)
        {
            try { await incrementalSync(); }
            catch { await ReloadAllPoliciesAsync(); }
        }

        /// <summary>
        /// 带全局写锁的全量策略重载。
        /// </summary>
        public async Task ReloadAllPoliciesAsync()
        {
            await _writeLock.WaitAsync();
            try { await _enforcer.LoadPolicyAsync(); }
            finally { _writeLock.Release(); }
        }

        public async Task AddRoleForUserAsync(User user, Role role)
        {
            string sub = GetUserSubject(user.Id);
            string roleSub = GetRoleSubject(role.RoleCode!);
            string domain = GetTenantDomain(user.TenantId);

            await _roleRepository._Db.Insertable(
                new CasbinRule { PType = "g", V0 = sub, V1 = roleSub, V2 = domain })
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try { await _enforcer.AddGroupingPolicyAsync(sub, roleSub, domain); }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task RemoveRoleForUserAsync(User user, Role role)
        {
            string sub = GetUserSubject(user.Id);
            string roleSub = GetRoleSubject(role.RoleCode!);
            string domain = GetTenantDomain(user.TenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain)
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try { await _enforcer.RemoveGroupingPolicyAsync(sub, roleSub, domain); }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task SetUserRolesAsync(User user, List<Role> roles)
        {
            string sub = GetUserSubject(user.Id);
            string domain = GetTenantDomain(user.TenantId);

            // DB（锁外，保留 domain 过滤）
            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
                .ExecuteCommandAsync();

            if (roles.Count > 0)
            {
                List<CasbinRule> newRules = roles.Select(r => new CasbinRule
                {
                    PType = "g", V0 = sub, V1 = GetRoleSubject(r.RoleCode!), V2 = domain
                }).ToList();
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
            }

            // 内存（锁内：枚举 + domain 过滤 + 逐个精确删除）
            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub)
                        .Select(r => r.ToList())
                        .Where(r => r.Count >= 3 && r[2] == domain)
                        .ToList();
                        
                    foreach (var rule in oldRules)
                    {
                        await _enforcer.RemoveGroupingPolicyAsync(rule[0], rule[1], rule[2]);
                    }

                    if (roles.Count > 0)
                    {
                        List<List<string>> groupings = roles.Select(r =>
                            new List<string> { sub, GetRoleSubject(r.RoleCode!), domain }).ToList();
                        await _enforcer.AddGroupingPoliciesAsync(groupings);
                    }
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
        {
            // P1/F-03: 超管保护 — 直接跳过，保持 *,* 通配符不变
            // 超管的权限由 InitAdminPermissionAsync 管理，不应被菜单分配覆盖
            if (string.Equals(role.RoleCode, _adminRoleCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string roleSub = GetRoleSubject(role.RoleCode!);
            string domain = GetTenantDomain(role.TenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                .ExecuteCommandAsync();

            List<CasbinRule> newRules = new();
            foreach (Menu menu in menus)
            {
                if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
                string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
                newRules.Add(new CasbinRule
                    { PType = "p", V0 = roleSub, V1 = domain, V2 = menu.ApiUrl, V3 = methods });
            }

            if (newRules.Count > 0)
                await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
                    if (menus.Count > 0)
                    {
                        List<List<string>> policies = new();
                        foreach (Menu menu in menus)
                        {
                            if (string.IsNullOrWhiteSpace(menu.ApiUrl)) continue;
                            string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;
                            policies.Add(new List<string> { roleSub, domain, menu.ApiUrl, methods });
                        }
                        await _enforcer.AddPoliciesAsync(policies);
                    }
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        /// <summary>
        /// 初始化超管通配符策略 p, &lt;adminRoleCode&gt;, &lt;domain&gt;, *, *
        /// 多租户场景：每个租户各自拥有独立的 admin 角色，方法为 adminRole 所属的 domain 创建 *,*
        /// 如果是全局跨租户 admin，调用方需遍历所有 domain 分别调用
        /// </summary>
        public async Task InitAdminPermissionAsync(Role adminRole)
        {
            string roleSub = GetRoleSubject(adminRole.RoleCode!);
            string domain = GetTenantDomain(adminRole.TenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                .ExecuteCommandAsync();

            await _roleRepository._Db.Insertable(new CasbinRule
                { PType = "p", V0 = roleSub, V1 = domain, V2 = "*", V3 = "*" })
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
                    await _enforcer.AddPolicyAsync(roleSub, domain, "*", "*");
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task CleanRolePoliciesAsync(Role role)
        {
            string roleSub = GetRoleSubject(role.RoleCode!);
            string domain = GetTenantDomain(role.TenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                         || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
                    await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task CleanRolePoliciesByRoleCodeAsync(string roleCode, Guid? tenantId)
        {
            string roleSub = GetRoleSubject(roleCode);
            string domain = GetTenantDomain(tenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                         || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _enforcer.RemoveFilteredPolicyAsync(0, roleSub, domain);
                    await _enforcer.RemoveFilteredGroupingPolicyAsync(1, roleSub, domain);
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }

        public async Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId)
        {
            string domain = GetTenantDomain(tenantId);

            await _roleRepository._Db.Updateable<CasbinRule>()
                .SetColumns(it => new CasbinRule { V1 = newRoleCode })
                .Where(x => x.PType == "g" && x.V1 == oldRoleCode && x.V2 == domain)
                .ExecuteCommandAsync();

            await _roleRepository._Db.Updateable<CasbinRule>()
                .SetColumns(it => new CasbinRule { V0 = newRoleCode })
                .Where(x => x.PType == "p" && x.V0 == oldRoleCode && x.V1 == domain)
                .ExecuteCommandAsync();

            Func<Task> syncAction = ReloadAllPoliciesAsync;

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await syncAction(); }
        }

        public async Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId)
        {
            string sub = GetUserSubject(userId);
            string domain = GetTenantDomain(tenantId);

            await _roleRepository._Db.Deleteable<CasbinRule>()
                .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
                .ExecuteCommandAsync();

            Func<Task> syncAction = async () =>
            {
                await _writeLock.WaitAsync();
                try
                {
                    var oldRules = _enforcer.GetFilteredGroupingPolicy(0, sub)
                        .Select(r => r.ToList())
                        .Where(r => r.Count >= 3 && r[2] == domain)
                        .ToList();
                    foreach (var rule in oldRules)
                    {
                        await _enforcer.RemoveGroupingPolicyAsync(rule[0], rule[1], rule[2]);
                    }
                }
                finally { _writeLock.Release(); }
            };

            IUnitOfWork? uow = _unitOfWorkManager.Current;
            if (uow != null) { uow.OnCompleted(syncAction); }
            else { await SyncOrFallback(syncAction); }
        }
    }
}
