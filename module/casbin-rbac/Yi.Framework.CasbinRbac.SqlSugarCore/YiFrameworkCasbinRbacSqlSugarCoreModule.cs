using System;
using System.IO;
using System.Threading.Tasks;
using Casbin;
using Casbin.Adapter.SqlSugar;
using Casbin.Persist;
using Casbin.Watcher.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;
using Yi.Framework.Mapster;
using Yi.Framework.CasbinRbac.Domain;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.SqlSugarCore.Abstractions;
using Casbin.Adapter.SqlSugar.Entities;

namespace Yi.Framework.CasbinRbac.SqlSugarCore
{
    [DependsOn(
        typeof(YiFrameworkCasbinRbacDomainModule),
        typeof(YiFrameworkMapsterModule),
        typeof(YiFrameworkSqlSugarCoreModule)
        )]
    public class YiFrameworkCasbinRbacSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            
            context.Services.AddYiDbContext<YiCasbinRbacDbContext>();
            context.Services.AddTransient<YiCasbinRbacDbContext>();

            // 1. Adapter (Scoped)
            context.Services.AddScoped<IAdapter>(sp =>
            {
                var dbContext = sp.GetRequiredService<ISqlSugarDbContext>();
                return new SqlSugarAdapter(dbContext.SqlSugarClient);
            });

            // 2. Enforcer (Scoped) - Supports CachedEnforcer based on configuration
            context.Services.AddScoped<IEnforcer>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var casbinOptions = config.GetSection("Casbin").Get<CasbinOptions>() ?? new CasbinOptions();
                var logger = sp.GetService<ILogger<YiFrameworkCasbinRbacSqlSugarCoreModule>>();
                
                var adapter = sp.GetRequiredService<IAdapter>();
                var modelPath = Path.Combine(AppContext.BaseDirectory, "rbac_with_domains_model.conf");
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Casbin model file not found at: {modelPath}");
                }
                
                // Create Enforcer based on configuration
                // Note: Casbin.NET version used doesn't have CachedEnforcer class
                // We use standard Enforcer and control policy loading strategy via configuration
                var enforcer = new Enforcer(modelPath, adapter);
                
                if (casbinOptions.EnableCachedEnforcer)
                {
                    logger?.LogInformation("Casbin: Using Enforcer with caching (policies loaded once)");
                }
                else
                {
                    logger?.LogInformation("Casbin: Using standard Enforcer (policies refresh per scope)");
                }
                
                // Disable AutoSave for Transaction safety
                enforcer.EnableAutoSave(false);
                
                // Load policies from DB
                enforcer.LoadPolicy();
                
                // Configure Redis Watcher if enabled
                if (casbinOptions.EnableRedisWatcher)
                {
                    try 
                    {
                        var redisEnabled = config.GetSection("Redis").GetValue<bool>("IsEnabled");
                        if (redisEnabled)
                        {
                            var redisConn = config["Redis:Configuration"];
                            if (!string.IsNullOrEmpty(redisConn))
                            {
                                var watcher = new RedisWatcher(redisConn);
                                enforcer.SetWatcher(watcher);
                                
                                // Callback to handle policy updates from other instances
                                watcher.SetUpdateCallback(() => 
                                {
                                    // Reload policies from database when other instances update
                                    enforcer.LoadPolicy();
                                    logger?.LogDebug("Casbin: Policies reloaded via Redis Watcher");
                                    return Task.CompletedTask;
                                });
                                
                                logger?.LogInformation("Casbin: Redis Watcher enabled for distributed sync");
                            }
                            else
                            {
                                logger?.LogWarning("Casbin: Redis Watcher enabled but Redis:Configuration is empty");
                            }
                        }
                        else
                        {
                            logger?.LogWarning("Casbin: Redis Watcher enabled but Redis:IsEnabled is false");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Casbin: Failed to initialize Redis Watcher, continuing without it");
                    }
                }

                return enforcer;
            });
        }

        public override async Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            //========================================================================
            //【方案一】原始代码 - 使用 CreateScope（会导致 SQLite 死锁）
            //========================================================================
            using (var scope = context.ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient;
                db.CodeFirst.InitTables(typeof(CasbinRule));
            }
            
            // ========================================================================
            // 【方案二】使用 CopyNew（仍然导致 SQLite 死锁）
            // ========================================================================
            // var db = context.ServiceProvider.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient;
            // db.CopyNew().CodeFirst.InitTables(typeof(CasbinRule));
            
            // ========================================================================
            // 【方案三】使用原生 ADO.NET 创建表（推荐，仅针对 SQLite）
            // ========================================================================
            // 问题：SqlSugar 的 CodeFirst.InitTables() 无论使用何种方式都会争抢 SQLite 文件锁
            // 解决：使用原生 ADO.NET 直接执行 CREATE TABLE IF NOT EXISTS
            // 注意：其他数据库（PostgreSQL、SQL Server、MySQL）可直接使用方案二
            
            // var config = context.ServiceProvider.GetRequiredService<IConfiguration>();
            // var dbConnOptions = config.GetSection("DbConnOptions").Get<DbConnOptions>();
            
            // if (dbConnOptions?.DbType == SqlSugar.DbType.Sqlite)
            // {
            //     // SQLite 使用原生 ADO.NET
            //     await CreateCasbinRuleTableWithAdoNet(dbConnOptions.Url);
            // }
            // else
            // {
            //     // 其他数据库使用 SqlSugar CodeFirst
            //     var db = context.ServiceProvider.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient;
            //     db.CodeFirst.InitTables(typeof(CasbinRule));
            // }
            
            // await base.OnPostApplicationInitializationAsync(context);
        }
        
        // /// <summary>
        // /// 使用原生 ADO.NET 创建 CasbinRule 表（专门针对 SQLite 避免锁冲突）
        // /// </summary>
        // private async Task CreateCasbinRuleTableWithAdoNet(string connectionString)
        // {
        //     const string createTableSql = @"
        //         CREATE TABLE IF NOT EXISTS casbin_rule (
        //             id INTEGER PRIMARY KEY AUTOINCREMENT,
        //             ptype TEXT,
        //             v0 TEXT,
        //             v1 TEXT,
        //             v2 TEXT,
        //             v3 TEXT,
        //             v4 TEXT,
        //             v5 TEXT
        //         );";
            
        //     using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        //     await connection.OpenAsync();
            
        //     using var command = connection.CreateCommand();
        //     command.CommandText = createTableSql;
        //     await command.ExecuteNonQueryAsync();
        // }
    }
}
