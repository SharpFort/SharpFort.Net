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
            context.Services.AddYiDbContext<YiCasbinRbacDbContext>();
            context.Services.AddTransient<YiCasbinRbacDbContext>();

            // 1. Adapter (Scoped)
            context.Services.AddScoped<IAdapter>(sp =>
            {
                var dbContext = sp.GetRequiredService<ISqlSugarDbContext>();
                return new SqlSugarAdapter(dbContext.SqlSugarClient);
            });

            // 2. Enforcer (Scoped)
            context.Services.AddScoped<IEnforcer>(sp =>
            {
                var adapter = sp.GetRequiredService<IAdapter>();
                var modelPath = Path.Combine(AppContext.BaseDirectory, "rbac_with_domains_model.conf");
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Casbin model file not found at: {modelPath}");
                }
                
                // Task 11: Use CachedEnforcer for better performance within the request scope
                var enforcer = new CachedEnforcer(modelPath, adapter);
                
                // Task 6: Disable AutoSave for Transaction safety
                enforcer.EnableAutoSave(false);
                
                // Load policies from DB (Scoped means this happens per request, ensuring fresh data)
                enforcer.LoadPolicy();
                
                // Task 10: Configure Redis Watcher
                var config = sp.GetRequiredService<IConfiguration>();
                var redisEnabled = config.GetSection("Redis").GetValue<bool>("IsEnabled");
                if (redisEnabled)
                {
                    try 
                    {
                        var redisConn = config["Redis:Configuration"];
                        // RedisWatcher (v2.0.0) usually takes connection string and channel name
                        // Warning: Creating a new RedisWatcher per request (Scoped) might be resource intensive (connections).
                        // Ideally, Watcher should be Singleton. 
                        // But Enforcer is Scoped. 
                        // SetWatcher binds them.
                        
                        // If we want to strictly follow "Scoped Enforcer", we might skip Watcher for *incoming* updates (since we reload anyway),
                        // but we need it for *publishing* updates (SavePolicy -> Watcher.Update).
                        
                        var watcher = new RedisWatcher(redisConn);
                        enforcer.SetWatcher(watcher);
                        
                        // Callback to clear cache if update received
                        // (Though Scoped Enforcer is short-lived, this is good practice)
                        watcher.SetUpdateCallback(() => 
                        {
                            enforcer.ClearPolicyCache(); // Clear CachedEnforcer cache
                            // We don't need LoadPolicy() here because Scoped Enforcer loads on creation.
                            // But if this is a long-running scope (unlikely in HTTP), we might.
                            return Task.CompletedTask;
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if Redis fails?
                         var logger = sp.GetService<ILogger<YiFrameworkCasbinRbacSqlSugarCoreModule>>();
                         logger?.LogWarning(ex, "Failed to initialize Casbin Redis Watcher.");
                    }
                }

                return enforcer;
            });
        }

        public override async Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            using (var scope = context.ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient;
                db.CodeFirst.InitTables(typeof(CasbinRule));
            }
            await base.OnPostApplicationInitializationAsync(context);
        }
    }
}
