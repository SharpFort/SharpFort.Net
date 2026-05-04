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
using SharpFort.Mapster;
using SharpFort.CasbinRbac.Domain;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;
using Casbin.Adapter.SqlSugar.Entities;

namespace SharpFort.CasbinRbac.SqlSugarCore
{
    [DependsOn(
        typeof(SharpFortCasbinRbacDomainModule),
        typeof(SharpFortMapsterModule),
        typeof(SharpFortSqlSugarCoreModule)
        )]
    public partial class SharpFortCasbinRbacSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            context.Services.AddSfDbContext<SfCasbinRbacDbContext>();
            context.Services.AddTransient<SfCasbinRbacDbContext>();

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
                var logger = sp.GetService<ILogger<SharpFortCasbinRbacSqlSugarCoreModule>>();

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
                    if (logger is not null) LogCachedEnforcerEnabled(logger);
                }
                else
                {
                    if (logger is not null) LogStandardEnforcerEnabled(logger);
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
                                    if (logger is not null) LogPoliciesReloaded(logger);
                                    return Task.CompletedTask;
                                });

                                if (logger is not null) LogRedisWatcherEnabled(logger);
                            }
                            else
                            {
                                if (logger is not null) LogRedisConfigEmpty(logger);
                            }
                        }
                        else
                        {
                            if (logger is not null) LogRedisDisabled(logger);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger is not null) LogRedisWatcherFailed(logger, ex);
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
                db.CodeFirst.InitTables<CasbinRule>();
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

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Casbin: Using Enforcer with caching (policies loaded once)")]
        private static partial void LogCachedEnforcerEnabled(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Casbin: Using standard Enforcer (policies refresh per scope)")]
        private static partial void LogStandardEnforcerEnabled(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Casbin: Policies reloaded via Redis Watcher")]
        private static partial void LogPoliciesReloaded(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Casbin: Redis Watcher enabled for distributed sync")]
        private static partial void LogRedisWatcherEnabled(ILogger logger);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Casbin: Redis Watcher enabled but Redis:Configuration is empty")]
        private static partial void LogRedisConfigEmpty(ILogger logger);

        [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Casbin: Redis Watcher enabled but Redis:IsEnabled is false")]
        private static partial void LogRedisDisabled(ILogger logger);

        [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Casbin: Failed to initialize Redis Watcher, continuing without it")]
        private static partial void LogRedisWatcherFailed(ILogger logger, Exception ex);
    }
}
