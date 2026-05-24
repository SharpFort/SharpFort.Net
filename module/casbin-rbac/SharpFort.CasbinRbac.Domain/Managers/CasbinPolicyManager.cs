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
        /// 延迟内存同步：事务运行期间仅进行 DB 持久化，不碰内存 Enforcer
        /// 事务成功提交后（OnCompleted），从 DB 全量重载策略到内存 (R-01)
        /// 非事务环境下则锁定后即时重载
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
                        await _writeLock.WaitAsync();
                        try
                        {
                            await _enforcer.LoadPolicyAsync();
                        }
                        finally
                        {
                            _writeLock.Release();
                        }
                    });
                }
            }
            else
            {
                _writeLock.Wait();
                try
                {
                    _enforcer.LoadPolicy();
                }
                finally
                {
                    _writeLock.Release();
                }
            }
        }

        public async Task AddRoleForUserAsync(User user, Role role)
        {
            await _writeLock.WaitAsync();
            try
            {
                string sub = GetUserSubject(user.Id);
                string roleSub = GetRoleSubject(role.RoleCode!);
                string domain = GetTenantDomain(user.TenantId);

                CasbinRule rule = new()
                {
                    PType = "g",
                    V0 = sub,
                    V1 = roleSub,
                    V2 = domain
                };
                await _roleRepository._Db.Insertable(rule).ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task RemoveRoleForUserAsync(User user, Role role)
        {
            await _writeLock.WaitAsync();
            try
            {
                string sub = GetUserSubject(user.Id);
                string roleSub = GetRoleSubject(role.RoleCode!);
                string domain = GetTenantDomain(user.TenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => x.PType == "g" && x.V0 == sub && x.V1 == roleSub && x.V2 == domain)
                    .ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task SetUserRolesAsync(User user, List<Role> roles)
        {
            await _writeLock.WaitAsync();
            try
            {
                string sub = GetUserSubject(user.Id);
                string domain = GetTenantDomain(user.TenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
                    .ExecuteCommandAsync();

                if (roles.Count > 0)
                {
                    List<CasbinRule> newRules = [.. roles.Select(r => new CasbinRule
                    {
                        PType = "g",
                        V0 = sub,
                        V1 = GetRoleSubject(r.RoleCode!),
                        V2 = domain
                    })];
                    await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
                }
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task SetRolePermissionsAsync(Role role, List<Menu> menus)
        {
            await _writeLock.WaitAsync();
            try
            {
                string roleSub = GetRoleSubject(role.RoleCode!);
                string domain = GetTenantDomain(role.TenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                    .ExecuteCommandAsync();

                List<CasbinRule> newRules = [];

                foreach (Menu menu in menus)
                {
                    if (string.IsNullOrWhiteSpace(menu.ApiUrl))
                    {
                        continue;
                    }

                    string methods = string.IsNullOrWhiteSpace(menu.ApiMethod) ? "*" : menu.ApiMethod;

                    newRules.Add(new CasbinRule
                    {
                        PType = "p",
                        V0 = roleSub,
                        V1 = domain,
                        V2 = menu.ApiUrl,
                        V3 = methods
                    });
                }

                if (newRules.Count > 0)
                {
                    await _roleRepository._Db.Insertable(newRules).ExecuteCommandAsync();
                }
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task InitAdminPermissionAsync(Role adminRole)
        {
            await _writeLock.WaitAsync();
            try
            {
                string roleSub = GetRoleSubject(adminRole.RoleCode!);
                string domain = GetTenantDomain(adminRole.TenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                    .ExecuteCommandAsync();

                await _roleRepository._Db.Insertable(new CasbinRule
                {
                    PType = "p",
                    V0 = roleSub,
                    V1 = domain,
                    V2 = "*",
                    V3 = "*"
                }).ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task CleanRolePoliciesAsync(Role role)
        {
            await _writeLock.WaitAsync();
            try
            {
                string roleSub = GetRoleSubject(role.RoleCode!);
                string domain = GetTenantDomain(role.TenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                             || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
                    .ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        public async Task CleanRolePoliciesByRoleCodeAsync(string roleCode, Guid? tenantId)
        {
            await _writeLock.WaitAsync();
            try
            {
                string roleSub = GetRoleSubject(roleCode);
                string domain = GetTenantDomain(tenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => (x.PType == "p" && x.V0 == roleSub && x.V1 == domain)
                             || (x.PType == "g" && x.V1 == roleSub && x.V2 == domain))
                    .ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        /// <summary>
        /// 迁移角色编码：将旧 RoleCode 的所有 p 规则和 g 规则更新为新 RoleCode (R-05)
        /// 使用数据库事务原子性更新，避免用户角色关联丢失
        /// </summary>
        public async Task MigrateRoleCodeAsync(string oldRoleCode, string newRoleCode, Guid? tenantId)
        {
            string domain = GetTenantDomain(tenantId);

            await _writeLock.WaitAsync();
            try
            {
                // 更新 g-rules: V1 从旧角色码变更为新角色码
                // 注意：SetColumns 必须使用初始化器语法进行赋值，== 是比较运算符 (QA5-CRITICAL-01)
                await _roleRepository._Db.Updateable<CasbinRule>()
                    .SetColumns(it => new CasbinRule() { V1 = newRoleCode })
                    .Where(x => x.PType == "g" && x.V1 == oldRoleCode && x.V2 == domain)
                    .ExecuteCommandAsync();

                // 更新 p-rules: V0 从旧角色码变更为新角色码
                await _roleRepository._Db.Updateable<CasbinRule>()
                    .SetColumns(it => new CasbinRule() { V0 = newRoleCode })
                    .Where(x => x.PType == "p" && x.V0 == oldRoleCode && x.V1 == domain)
                    .ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }

        /// <summary>
        /// 清理用户所有的 Casbin 策略（删除用户时调用）(B-08)
        /// </summary>
        public async Task CleanUserPoliciesAsync(Guid userId, Guid? tenantId)
        {
            await _writeLock.WaitAsync();
            try
            {
                string sub = GetUserSubject(userId);
                string domain = GetTenantDomain(tenantId);

                await _roleRepository._Db.Deleteable<CasbinRule>()
                    .Where(x => x.PType == "g" && x.V0 == sub && x.V2 == domain)
                    .ExecuteCommandAsync();
            }
            finally
            {
                _writeLock.Release();
            }

            TriggerMemorySync();
        }
    }
}
