using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Casbin;
using Casbin.Adapter.SqlSugar.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.Domain.Services;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    /// <summary>
    /// Casbin Data Migration Service
    /// Uses COMPLETELY DECOUPLED read/write phases to avoid SQLite lock conflicts
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
        /// Phase 1: Read all data with dedicated connection, then DISPOSE immediately
        /// Phase 2: Build rules in memory (no DB access)
        /// Phase 3: Write with NEW dedicated connection
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
            
            List<Role> roles;
            List<Menu> menus;
            List<RoleMenu> roleMenus;
            List<UserRole> userRoles;

            // Create dedicated READ client
            using (var readClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            }))
            {
                // NOTE: No PRAGMA commands in READ phase
                // PRAGMA journal_mode=WAL requires exclusive lock, causing blocking
                // NOTE: Use IgnoreColumns to skip ABP's ExtraProperties/ConcurrencyStamp (not in SQLite table)
                var stepSw = Stopwatch.StartNew();
                _logger.LogInformation("[READ] Reading Role table...");
                roles = await readClient.Queryable<Role>()
                    .IgnoreColumns("ExtraProperties", "ConcurrencyStamp")
                    .ToListAsync();
                _logger.LogInformation($"[READ] Role: {roles.Count} rows, {stepSw.ElapsedMilliseconds}ms");

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading Menu table...");
                menus = await readClient.Queryable<Menu>()
                    .IgnoreColumns("ExtraProperties", "ConcurrencyStamp")
                    .ToListAsync();
                _logger.LogInformation($"[READ] Menu: {menus.Count} rows, {stepSw.ElapsedMilliseconds}ms");

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading RoleMenu table...");
                roleMenus = await readClient.Queryable<RoleMenu>().ToListAsync();
                _logger.LogInformation($"[READ] RoleMenu: {roleMenus.Count} rows, {stepSw.ElapsedMilliseconds}ms");

                stepSw.Restart();
                _logger.LogInformation("[READ] Reading UserRole table...");
                userRoles = await readClient.Queryable<UserRole>().ToListAsync();
                _logger.LogInformation($"[READ] UserRole: {userRoles.Count} rows, {stepSw.ElapsedMilliseconds}ms");
            }
            // *** READ CLIENT IS NOW DISPOSED ***
            _logger.LogInformation($"[PHASE 1] READ phase COMPLETE. Connection CLOSED. Total: {phaseSw.ElapsedMilliseconds}ms");
            
            // Small delay to ensure SQLite releases file lock
            await Task.Delay(100);

            // ========== PHASE 2: BUILD RULES IN MEMORY ==========
            _logger.LogInformation("[PHASE 2] Starting PROCESSING phase (in-memory)...");
            phaseSw.Restart();

            var roleDic = new Dictionary<Guid, Role>();
            foreach (var r in roles) roleDic[r.Id] = r;

            var menuDic = new Dictionary<Guid, Menu>();
            foreach (var m in menus) menuDic[m.Id] = m;

            var rulesToInsert = new List<CasbinRule>();

            // Build p rules (role-permission)
            foreach (var rm in roleMenus)
            {
                if (!roleDic.TryGetValue(rm.RoleId, out var role)) continue;
                if (!menuDic.TryGetValue(rm.MenuId, out var menu)) continue;
                if (string.IsNullOrEmpty(menu.ApiUrl)) continue;

                var method = string.IsNullOrEmpty(menu.ApiMethod) ? "*" : menu.ApiMethod.ToUpper();

                rulesToInsert.Add(new CasbinRule
                {
                    PType = "p",
                    V0 = role.Id.ToString(), // Use roleId for consistency with RoleService
                    V1 = domain,
                    V2 = menu.ApiUrl,
                    V3 = method
                });
            }
            int pRuleCount = rulesToInsert.Count;
            _logger.LogInformation($"[PROCESS] Built {pRuleCount} p-rules (role-permission)");

            // Build g rules (user-role)
            foreach (var ur in userRoles)
            {
                if (!roleDic.TryGetValue(ur.RoleId, out var role)) continue;

                rulesToInsert.Add(new CasbinRule
                {
                    PType = "g",
                    V0 = ur.UserId.ToString(), // Use userId directly
                    V1 = ur.RoleId.ToString(), // Use roleId for consistency
                    V2 = domain
                });
            }
            int gRuleCount = rulesToInsert.Count - pRuleCount;
            _logger.LogInformation($"[PROCESS] Built {gRuleCount} g-rules (user-role)");
            _logger.LogInformation($"[PHASE 2] PROCESSING phase COMPLETE. Total rules: {rulesToInsert.Count}, {phaseSw.ElapsedMilliseconds}ms");

            // ========== PHASE 3: GENERATE SQL FILE ==========
            // SQLite's file-level lock prevents writing while Enforcer's adapter holds a connection
            // Solution: Generate SQL file for manual execution when app is stopped
            _logger.LogInformation("[PHASE 3] Generating SQL file (bypassing SQLite lock)...");
            phaseSw.Restart();

            var sqlFilePath = Path.Combine(AppContext.BaseDirectory, $"casbin_migration_{DateTime.Now:yyyyMMdd_HHmmss}.sql");
            var sb = new StringBuilder();
            
            sb.AppendLine("-- Casbin Migration SQL");
            sb.AppendLine($"-- Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"-- Total rules: {rulesToInsert.Count}");
            sb.AppendLine();
            sb.AppendLine("-- Step 1: Clear old data");
            sb.AppendLine("DELETE FROM casbin_rule;");
            sb.AppendLine();
            sb.AppendLine("-- Step 2: Insert new rules");
            
            foreach (var rule in rulesToInsert)
            {
                var v0 = rule.V0?.Replace("'", "''") ?? "";
                var v1 = rule.V1?.Replace("'", "''") ?? "";
                var v2 = rule.V2?.Replace("'", "''") ?? "";
                var v3 = rule.V3?.Replace("'", "''") ?? "";
                var v4 = rule.V4?.Replace("'", "''") ?? "";
                var v5 = rule.V5?.Replace("'", "''") ?? "";
                
                sb.AppendLine($"INSERT INTO casbin_rule (PType, V0, V1, V2, V3, V4, V5) VALUES ('{rule.PType}', '{v0}', '{v1}', '{v2}', '{v3}', '{v4}', '{v5}');");
            }
            
            await File.WriteAllTextAsync(sqlFilePath, sb.ToString());
            _logger.LogInformation($"[PHASE 3] SQL file generated: {sqlFilePath}");
            _logger.LogInformation($"[PHASE 3] COMPLETE. {rulesToInsert.Count} INSERT statements, {phaseSw.ElapsedMilliseconds}ms");

            // ========== PHASE 4: SKIP ENFORCER RELOAD ==========
            _logger.LogWarning("[PHASE 4] SKIPPED - SQL file needs manual execution");
            _logger.LogWarning($"[ACTION REQUIRED] 1. Stop the application");
            _logger.LogWarning($"[ACTION REQUIRED] 2. Execute SQL file: {sqlFilePath}");
            _logger.LogWarning($"[ACTION REQUIRED] 3. Restart the application");

            totalSw.Stop();
            _logger.LogInformation($"========== CASBIN MIGRATION SQL GENERATED. Total time: {totalSw.ElapsedMilliseconds}ms ==========");
        }
    }
}

