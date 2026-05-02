using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Casbin;
using Casbin.Adapter.SqlSugar.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.Domain.Services;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    /// <summary>
    /// Casbin Data Migration Service
    /// Migrates role, menu, and user-role data to Casbin policy table
    /// </summary>
    public class CasbinSeedService : DomainService
    {
        private readonly IEnforcer _enforcer;
        private readonly ISqlSugarRepository<Role> _roleRepo;
        private readonly ILogger<CasbinSeedService> _logger;

        public CasbinSeedService(
            IEnforcer enforcer,
            ISqlSugarRepository<Role> roleRepo,
            ILogger<CasbinSeedService> logger)
        {
            _enforcer = enforcer;
            _roleRepo = roleRepo;
            _logger = logger;
        }

        /// <summary>
        /// Perform Full Migration with COMPLETELY DECOUPLED phases
        /// Phase 1: Read all data with dedicated connection
        /// Phase 2: Build rules in memory (no DB access)
        /// Phase 3: Write with NEW dedicated connection
        /// Phase 4: Reload enforcer
        /// </summary>
        [Volo.Abp.Uow.UnitOfWork(IsDisabled = true)]
        public async Task MigrateAllAsync()
        {
            var totalSw = Stopwatch.StartNew();
            _logger.LogInformation("========== CASBIN MIGRATION START ==========");

            var connectionString = _roleRepo._Db.CurrentConnectionConfig.ConnectionString;
            var dbType = _roleRepo._Db.CurrentConnectionConfig.DbType;
            string domain = "default";

            // ========== PHASE 1: READ DATA ==========
            _logger.LogInformation("[PHASE 1] Starting READ phase...");
            var phaseSw = Stopwatch.StartNew();

            // Use anonymous types to avoid protected setter issues
            List<(Guid Id, string RoleCode, string RoleName, bool State)> roleData;
            List<(Guid Id, string MenuName, string ApiUrl, string ApiMethod, bool State)> menuData;
            List<(Guid RoleId, Guid MenuId)> roleMenuData;
            List<(Guid UserId, Guid RoleId)> userRoleData;

            // Create dedicated READ client
            using (var readClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            }))
            {
                var stepSw = Stopwatch.StartNew();
                _logger.LogInformation("[READ] Reading Role table...");

                // Use raw SQL to avoid entity mapping issues with TenantId field
                // We only need: id, role_code, role_name, state (no tenant_id needed)
                var roleQuery = @"
                    SELECT id, role_code, role_name, state
                    FROM casbin_sys_role
                    WHERE is_deleted = false";

                var roleRows = await readClient.Ado.SqlQueryAsync<dynamic>(roleQuery);
                roleData = roleRows.Select(r => (
                    Id: (Guid)r.id,
                    RoleCode: (string)r.role_code,
                    RoleName: (string)r.role_name,
                    State: (bool)r.state
                )).ToList();

                _logger.LogInformation("[READ] Role: {Count} rows, {ElapsedMs}ms", roleData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading Menu table...");

                // Use raw SQL for Menu table
                var menuQuery = @"
                    SELECT id, menu_name, api_url, api_method, state
                    FROM casbin_sys_menu
                    WHERE is_deleted = false";

                var menuRows = await readClient.Ado.SqlQueryAsync<dynamic>(menuQuery);
                menuData = menuRows.Select(m => (
                    Id: (Guid)m.id,
                    MenuName: (string)m.menu_name,
                    ApiUrl: m.api_url != null ? (string)m.api_url : "",
                    ApiMethod: m.api_method != null ? (string)m.api_method : "",
                    State: (bool)m.state
                )).ToList();

                _logger.LogInformation("[READ] Menu: {Count} rows, {ElapsedMs}ms", menuData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading RoleMenu table...");

                // Use raw SQL for RoleMenu table
                var roleMenuQuery = @"
                    SELECT role_id, menu_id
                    FROM casbin_sys_role_menu";

                var roleMenuRows = await readClient.Ado.SqlQueryAsync<dynamic>(roleMenuQuery);
                roleMenuData = roleMenuRows.Select(rm => (
                    RoleId: (Guid)rm.role_id,
                    MenuId: (Guid)rm.menu_id
                )).ToList();

                _logger.LogInformation("[READ] RoleMenu: {Count} rows, {ElapsedMs}ms", roleMenuData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading UserRole table...");

                // Use raw SQL for UserRole table
                var userRoleQuery = @"
                    SELECT user_id, role_id
                    FROM casbin_sys_user_role";

                var userRoleRows = await readClient.Ado.SqlQueryAsync<dynamic>(userRoleQuery);
                userRoleData = userRoleRows.Select(ur => (
                    UserId: (Guid)ur.user_id,
                    RoleId: (Guid)ur.role_id
                )).ToList();

                _logger.LogInformation("[READ] UserRole: {Count} rows, {ElapsedMs}ms", userRoleData.Count, stepSw.ElapsedMilliseconds);
            }
            // *** READ CLIENT IS NOW DISPOSED ***
            _logger.LogInformation("[PHASE 1] READ phase COMPLETE. Total: {ElapsedMs}ms", phaseSw.ElapsedMilliseconds);

            // Small delay to ensure connection is fully released
            await Task.Delay(100);

            // ========== PHASE 2: BUILD RULES IN MEMORY ==========
            _logger.LogInformation("[PHASE 2] Starting PROCESSING phase (in-memory)...");
            phaseSw.Restart();

            // Build dictionaries for fast lookup
            var roleDic = new Dictionary<Guid, (string RoleCode, string RoleName)>();
            foreach (var r in roleData)
            {
                if (!string.IsNullOrEmpty(r.RoleCode))
                {
                    roleDic[r.Id] = (r.RoleCode, r.RoleName);
                }
                else
                {
                    _logger.LogWarning("[PROCESS] Skipping role {RoleId} - RoleCode is empty", r.Id);
                }
            }
            _logger.LogInformation("[PROCESS] Loaded {Count} valid roles", roleDic.Count);

            var menuDic = new Dictionary<Guid, (string MenuName, string ApiUrl, string ApiMethod)>();
            int menuWithApiCount = 0;
            foreach (var m in menuData)
            {
                menuDic[m.Id] = (m.MenuName, m.ApiUrl, m.ApiMethod);
                if (!string.IsNullOrEmpty(m.ApiUrl))
                {
                    menuWithApiCount++;
                }
            }
            _logger.LogInformation("[PROCESS] Loaded {MenuCount} menus, {ApiCount} with API URLs", menuDic.Count, menuWithApiCount);

            var rulesToInsert = new List<CasbinRule>();

            // Build p rules (role-permission)
            _logger.LogInformation("[PROCESS] Building p-rules (role-permission)...");
            int skippedMenus = 0;
            int skippedRoles = 0;

            foreach (var rm in roleMenuData)
            {
                if (!roleDic.TryGetValue(rm.RoleId, out var role))
                {
                    skippedRoles++;
                    continue;
                }

                if (!menuDic.TryGetValue(rm.MenuId, out var menu))
                {
                    skippedMenus++;
                    continue;
                }

                // Only create rules for menus with API URLs
                if (string.IsNullOrEmpty(menu.ApiUrl))
                {
                    continue;
                }

                var method = string.IsNullOrEmpty(menu.ApiMethod) ? "*" : menu.ApiMethod.ToUpper(global::System.Globalization.CultureInfo.InvariantCulture);

                rulesToInsert.Add(new CasbinRule
                {
                    PType = "p",
                    V0 = role.RoleCode, // Use RoleCode for better readability
                    V1 = domain,
                    V2 = menu.ApiUrl,
                    V3 = method
                });
            }

            int pRuleCount = rulesToInsert.Count;
            _logger.LogInformation("[PROCESS] Built {Count} p-rules (role-permission)", pRuleCount);
            if (skippedRoles > 0) _logger.LogWarning("[PROCESS] Skipped {Count} role-menu relations (role not found)", skippedRoles);
            if (skippedMenus > 0) _logger.LogWarning("[PROCESS] Skipped {Count} role-menu relations (menu not found)", skippedMenus);

            // Build g rules (user-role)
            _logger.LogInformation("[PROCESS] Building g-rules (user-role)...");
            int skippedUserRoles = 0;

            foreach (var ur in userRoleData)
            {
                if (!roleDic.TryGetValue(ur.RoleId, out var role))
                {
                    skippedUserRoles++;
                    continue;
                }

                rulesToInsert.Add(new CasbinRule
                {
                    PType = "g",
                    V0 = ur.UserId.ToString(), // Use userId directly (no prefix)
                    V1 = role.RoleCode, // Use RoleCode for consistency
                    V2 = domain
                });
            }

            int gRuleCount = rulesToInsert.Count - pRuleCount;
            _logger.LogInformation("[PROCESS] Built {Count} g-rules (user-role)", gRuleCount);
            if (skippedUserRoles > 0) _logger.LogWarning("[PROCESS] Skipped {Count} user-role relations (role not found)", skippedUserRoles);

            _logger.LogInformation("[PHASE 2] PROCESSING COMPLETE. Total rules: {TotalRules}, {ElapsedMs}ms", rulesToInsert.Count, phaseSw.ElapsedMilliseconds);

            // Log sample data for verification
            _logger.LogInformation("[PHASE 2] Sample p-rules:");
            foreach (var rule in rulesToInsert.Where(r => r.PType == "p").Take(5))
            {
                _logger.LogInformation("  p, {V0}, {V1}, {V2}, {V3}", rule.V0, rule.V1, rule.V2, rule.V3);
            }

            _logger.LogInformation("[PHASE 2] Sample g-rules:");
            foreach (var rule in rulesToInsert.Where(r => r.PType == "g").Take(5))
            {
                _logger.LogInformation("  g, {V0}, {V1}, {V2}", rule.V0, rule.V1, rule.V2);
            }

            // ========== PHASE 3: WRITE TO DATABASE ==========
            _logger.LogInformation("[PHASE 3] Starting WRITE phase...");
            phaseSw.Restart();

            if (rulesToInsert.Count == 0)
            {
                _logger.LogWarning("[PHASE 3] No rules to insert! Check your role-menu and user-role configurations.");
                _logger.LogInformation("========== CASBIN MIGRATION COMPLETED (NO DATA). Total time: {ElapsedMs}ms ==========", totalSw.ElapsedMilliseconds);
                return;
            }

            // Create dedicated WRITE client
            using (var writeClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    EntityService = (property, column) =>
                    {
                        // Force convert PascalCase to snake_case for PostgreSQL
                        // This is critical for casbin_rule table (PType -> p_type, etc.)
                        column.DbColumnName = ToSnakeCase(property.Name);
                    }
                }
            }))
            {
                try
                {
                    // Clear old data
                    _logger.LogInformation("[WRITE] Clearing old casbin_rule data...");
                    var deletedCount = await writeClient.Deleteable<CasbinRule>().ExecuteCommandAsync();
                    _logger.LogInformation("[WRITE] Deleted {Count} old rules.", deletedCount);

                    // Insert new rules in batches
                    _logger.LogInformation("[WRITE] Inserting {Count} new rules...", rulesToInsert.Count);
                    var batchSize = 500;
                    int totalInserted = 0;

                    for (int i = 0; i < rulesToInsert.Count; i += batchSize)
                    {
                        var batch = rulesToInsert.Skip(i).Take(batchSize).ToList();
                        var insertedCount = await writeClient.Insertable(batch).ExecuteCommandAsync();
                        totalInserted += insertedCount;
                        _logger.LogInformation("[WRITE] Batch {BatchNum}: inserted {Count} rules (total: {Total}/{Max})", i / batchSize + 1, insertedCount, totalInserted, rulesToInsert.Count);
                    }

                    _logger.LogInformation("[WRITE] All {Count} rules inserted successfully.", totalInserted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WRITE] Failed to write casbin_rule data");
                    throw;
                }
            }
            // *** WRITE CLIENT IS NOW DISPOSED ***
            _logger.LogInformation("[PHASE 3] WRITE phase COMPLETE. {Count} rules inserted, {ElapsedMs}ms", rulesToInsert.Count, phaseSw.ElapsedMilliseconds);

            // ========== PHASE 4: RELOAD ENFORCER ==========
            _logger.LogInformation("[PHASE 4] Reloading Casbin Enforcer...");
            phaseSw.Restart();

            try
            {
                await _enforcer.LoadPolicyAsync();
                _logger.LogInformation("[PHASE 4] Enforcer reloaded successfully. {ElapsedMs}ms", phaseSw.ElapsedMilliseconds);

                // Verify loaded policies (use synchronous methods)
                var loadedPolicies = _enforcer.GetPolicy().ToList();
                var loadedGroupings = _enforcer.GetGroupingPolicy().ToList();
                _logger.LogInformation("[PHASE 4] Verification: {PolicyCount} policies, {GroupingCount} groupings loaded", loadedPolicies.Count, loadedGroupings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PHASE 4] Failed to reload Casbin Enforcer");
                throw;
            }

            totalSw.Stop();
            _logger.LogInformation("========== CASBIN MIGRATION COMPLETED SUCCESSFULLY. Total time: {ElapsedMs}ms ==========", totalSw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Convert PascalCase to snake_case
        /// Example: TenantId -> tenant_id, RoleCode -> role_code
        /// </summary>
        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLowerInvariant(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsUpper(c))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}

