using System;
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
    public partial class CasbinSeedService : DomainService
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
            LogMigrationStart();

            var connectionString = _roleRepo._Db.CurrentConnectionConfig.ConnectionString;
            var dbType = _roleRepo._Db.CurrentConnectionConfig.DbType;
            string domain = "default";

            // ========== PHASE 1: READ DATA ==========
            LogReadPhaseStart();
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
                LogReadingRoleTable();

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

                LogTableReadComplete("Role", roleData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                LogReadingMenuTable();

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

                LogTableReadComplete("Menu", menuData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                LogReadingRoleMenuTable();

                // Use raw SQL for RoleMenu table
                var roleMenuQuery = @"
                    SELECT role_id, menu_id
                    FROM casbin_sys_role_menu";

                var roleMenuRows = await readClient.Ado.SqlQueryAsync<dynamic>(roleMenuQuery);
                roleMenuData = roleMenuRows.Select(rm => (
                    RoleId: (Guid)rm.role_id,
                    MenuId: (Guid)rm.menu_id
                )).ToList();

                LogTableReadComplete("RoleMenu", roleMenuData.Count, stepSw.ElapsedMilliseconds);

                stepSw.Restart();
                LogReadingUserRoleTable();

                // Use raw SQL for UserRole table
                var userRoleQuery = @"
                    SELECT user_id, role_id
                    FROM casbin_sys_user_role";

                var userRoleRows = await readClient.Ado.SqlQueryAsync<dynamic>(userRoleQuery);
                userRoleData = userRoleRows.Select(ur => (
                    UserId: (Guid)ur.user_id,
                    RoleId: (Guid)ur.role_id
                )).ToList();

                LogTableReadComplete("UserRole", userRoleData.Count, stepSw.ElapsedMilliseconds);
            }
            // *** READ CLIENT IS NOW DISPOSED ***
            LogReadPhaseComplete(phaseSw.ElapsedMilliseconds);

            // Small delay to ensure connection is fully released
            await Task.Delay(100);

            // ========== PHASE 2: BUILD RULES IN MEMORY ==========
            LogProcessPhaseStart();
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
                    LogSkippingEmptyRoleCode(r.Id);
                }
            }
            LogValidRolesLoaded(roleDic.Count);

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
            LogMenusLoaded(menuDic.Count, menuWithApiCount);

            var rulesToInsert = new List<CasbinRule>();

            // Build p rules (role-permission)
            LogBuildingPRules();
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
            LogPRulesBuilt(pRuleCount);
            if (skippedRoles > 0) LogSkippedRoleMenuRoleNotFound(skippedRoles);
            if (skippedMenus > 0) LogSkippedRoleMenuMenuNotFound(skippedMenus);

            // Build g rules (user-role)
            LogBuildingGRules();
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
            LogGRulesBuilt(gRuleCount);
            if (skippedUserRoles > 0) LogSkippedUserRoleNotFound(skippedUserRoles);

            LogProcessPhaseComplete(rulesToInsert.Count, phaseSw.ElapsedMilliseconds);

            // Log sample data for verification
            LogSamplePRulesHeader();
            foreach (var rule in rulesToInsert.Where(r => r.PType == "p").Take(5))
            {
                LogSamplePRule(rule.V0, rule.V1, rule.V2, rule.V3);
            }

            LogSampleGRulesHeader();
            foreach (var rule in rulesToInsert.Where(r => r.PType == "g").Take(5))
            {
                LogSampleGRule(rule.V0, rule.V1, rule.V2);
            }

            // ========== PHASE 3: WRITE TO DATABASE ==========
            LogWritePhaseStart();
            phaseSw.Restart();

            if (rulesToInsert.Count == 0)
            {
                LogNoRulesToInsert();
                LogMigrationCompletedNoData(totalSw.ElapsedMilliseconds);
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
                    LogClearingOldRules();
                    var deletedCount = await writeClient.Deleteable<CasbinRule>().ExecuteCommandAsync();
                    LogOldRulesDeleted(deletedCount);

                    // Insert new rules in batches
                    LogInsertingRules(rulesToInsert.Count);
                    var batchSize = 500;
                    int totalInserted = 0;

                    for (int i = 0; i < rulesToInsert.Count; i += batchSize)
                    {
                        var batch = rulesToInsert.Skip(i).Take(batchSize).ToList();
                        var insertedCount = await writeClient.Insertable(batch).ExecuteCommandAsync();
                        totalInserted += insertedCount;
                        LogBatchInserted(i / batchSize + 1, insertedCount, totalInserted, rulesToInsert.Count);
                    }

                    LogAllRulesInserted(totalInserted);
                }
                catch (Exception ex)
                {
                    LogWriteFailed(ex);
                    throw;
                }
            }
            // *** WRITE CLIENT IS NOW DISPOSED ***
            LogWritePhaseComplete(rulesToInsert.Count, phaseSw.ElapsedMilliseconds);

            // ========== PHASE 4: RELOAD ENFORCER ==========
            LogReloadingEnforcer();
            phaseSw.Restart();

            try
            {
                await _enforcer.LoadPolicyAsync();
                LogEnforcerReloaded(phaseSw.ElapsedMilliseconds);

                // Verify loaded policies (use synchronous methods)
                var loadedPolicies = _enforcer.GetPolicy().ToList();
                var loadedGroupings = _enforcer.GetGroupingPolicy().ToList();
                LogVerificationResult(loadedPolicies.Count, loadedGroupings.Count);
            }
            catch (Exception ex)
            {
                LogReloadFailed(ex);
                throw;
            }

            totalSw.Stop();
            LogMigrationCompleted(totalSw.ElapsedMilliseconds);
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

        #region LoggerMessage Definitions

        // Migration lifecycle
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "========== CASBIN MIGRATION START ==========")]
        private partial void LogMigrationStart();

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "========== CASBIN MIGRATION COMPLETED SUCCESSFULLY. Total time: {ElapsedMs}ms ==========")]
        private partial void LogMigrationCompleted(long elapsedMs);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "========== CASBIN MIGRATION COMPLETED (NO DATA). Total time: {ElapsedMs}ms ==========")]
        private partial void LogMigrationCompletedNoData(long elapsedMs);

        // Phase 1 - Read
        [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "[PHASE 1] Starting READ phase...")]
        private partial void LogReadPhaseStart();

        [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "[PHASE 1] READ phase COMPLETE. Total: {ElapsedMs}ms")]
        private partial void LogReadPhaseComplete(long elapsedMs);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "[READ] Reading Role table...")]
        private partial void LogReadingRoleTable();

        [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "[READ] {TableName}: {Count} rows, {ElapsedMs}ms")]
        private partial void LogTableReadComplete(string tableName, int count, long elapsedMs);

        [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "[READ] Reading Menu table...")]
        private partial void LogReadingMenuTable();

        [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "[READ] Reading RoleMenu table...")]
        private partial void LogReadingRoleMenuTable();

        [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "[READ] Reading UserRole table...")]
        private partial void LogReadingUserRoleTable();

        // Phase 2 - Process
        [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "[PHASE 2] Starting PROCESSING phase (in-memory)...")]
        private partial void LogProcessPhaseStart();

        [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "[PROCESS] Skipping role {RoleId} - RoleCode is empty")]
        private partial void LogSkippingEmptyRoleCode(Guid roleId);

        [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "[PROCESS] Loaded {Count} valid roles")]
        private partial void LogValidRolesLoaded(int count);

        [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "[PROCESS] Loaded {MenuCount} menus, {ApiCount} with API URLs")]
        private partial void LogMenusLoaded(int menuCount, int apiCount);

        [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "[PROCESS] Building p-rules (role-permission)...")]
        private partial void LogBuildingPRules();

        [LoggerMessage(EventId = 25, Level = LogLevel.Information, Message = "[PROCESS] Built {Count} p-rules (role-permission)")]
        private partial void LogPRulesBuilt(int count);

        [LoggerMessage(EventId = 26, Level = LogLevel.Warning, Message = "[PROCESS] Skipped {Count} role-menu relations (role not found)")]
        private partial void LogSkippedRoleMenuRoleNotFound(int count);

        [LoggerMessage(EventId = 27, Level = LogLevel.Warning, Message = "[PROCESS] Skipped {Count} role-menu relations (menu not found)")]
        private partial void LogSkippedRoleMenuMenuNotFound(int count);

        [LoggerMessage(EventId = 28, Level = LogLevel.Information, Message = "[PROCESS] Building g-rules (user-role)...")]
        private partial void LogBuildingGRules();

        [LoggerMessage(EventId = 29, Level = LogLevel.Information, Message = "[PROCESS] Built {Count} g-rules (user-role)")]
        private partial void LogGRulesBuilt(int count);

        [LoggerMessage(EventId = 30, Level = LogLevel.Warning, Message = "[PROCESS] Skipped {Count} user-role relations (role not found)")]
        private partial void LogSkippedUserRoleNotFound(int count);

        [LoggerMessage(EventId = 31, Level = LogLevel.Information, Message = "[PHASE 2] PROCESSING COMPLETE. Total rules: {TotalRules}, {ElapsedMs}ms")]
        private partial void LogProcessPhaseComplete(int totalRules, long elapsedMs);

        [LoggerMessage(EventId = 32, Level = LogLevel.Information, Message = "[PHASE 2] Sample p-rules:")]
        private partial void LogSamplePRulesHeader();

        [LoggerMessage(EventId = 33, Level = LogLevel.Information, Message = "  p, {V0}, {V1}, {V2}, {V3}")]
        private partial void LogSamplePRule(string v0, string v1, string v2, string v3);

        [LoggerMessage(EventId = 34, Level = LogLevel.Information, Message = "[PHASE 2] Sample g-rules:")]
        private partial void LogSampleGRulesHeader();

        [LoggerMessage(EventId = 35, Level = LogLevel.Information, Message = "  g, {V0}, {V1}, {V2}")]
        private partial void LogSampleGRule(string v0, string v1, string v2);

        // Phase 3 - Write
        [LoggerMessage(EventId = 40, Level = LogLevel.Information, Message = "[PHASE 3] Starting WRITE phase...")]
        private partial void LogWritePhaseStart();

        [LoggerMessage(EventId = 41, Level = LogLevel.Warning, Message = "[PHASE 3] No rules to insert! Check your role-menu and user-role configurations.")]
        private partial void LogNoRulesToInsert();

        [LoggerMessage(EventId = 42, Level = LogLevel.Information, Message = "[WRITE] Clearing old casbin_rule data...")]
        private partial void LogClearingOldRules();

        [LoggerMessage(EventId = 43, Level = LogLevel.Information, Message = "[WRITE] Deleted {Count} old rules.")]
        private partial void LogOldRulesDeleted(int count);

        [LoggerMessage(EventId = 44, Level = LogLevel.Information, Message = "[WRITE] Inserting {Count} new rules...")]
        private partial void LogInsertingRules(int count);

        [LoggerMessage(EventId = 45, Level = LogLevel.Information, Message = "[WRITE] Batch {BatchNum}: inserted {Count} rules (total: {Total}/{Max})")]
        private partial void LogBatchInserted(int batchNum, int count, int total, int max);

        [LoggerMessage(EventId = 46, Level = LogLevel.Information, Message = "[WRITE] All {Count} rules inserted successfully.")]
        private partial void LogAllRulesInserted(int count);

        [LoggerMessage(EventId = 47, Level = LogLevel.Error, Message = "[WRITE] Failed to write casbin_rule data")]
        private partial void LogWriteFailed(Exception ex);

        [LoggerMessage(EventId = 48, Level = LogLevel.Information, Message = "[PHASE 3] WRITE phase COMPLETE. {Count} rules inserted, {ElapsedMs}ms")]
        private partial void LogWritePhaseComplete(int count, long elapsedMs);

        // Phase 4 - Reload
        [LoggerMessage(EventId = 50, Level = LogLevel.Information, Message = "[PHASE 4] Reloading Casbin Enforcer...")]
        private partial void LogReloadingEnforcer();

        [LoggerMessage(EventId = 51, Level = LogLevel.Information, Message = "[PHASE 4] Enforcer reloaded successfully. {ElapsedMs}ms")]
        private partial void LogEnforcerReloaded(long elapsedMs);

        [LoggerMessage(EventId = 52, Level = LogLevel.Information, Message = "[PHASE 4] Verification: {PolicyCount} policies, {GroupingCount} groupings loaded")]
        private partial void LogVerificationResult(int policyCount, int groupingCount);

        [LoggerMessage(EventId = 53, Level = LogLevel.Error, Message = "[PHASE 4] Failed to reload Casbin Enforcer")]
        private partial void LogReloadFailed(Exception ex);

        #endregion
    }
}
