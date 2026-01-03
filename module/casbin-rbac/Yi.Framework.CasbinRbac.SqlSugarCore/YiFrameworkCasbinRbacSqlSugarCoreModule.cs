using System;
using System.IO;
using Casbin;
using Casbin.Adapter.SqlSugar;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Yi.Framework.Mapster;
using Yi.Framework.CasbinRbac.Domain;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.CasbinRbac.SqlSugarCore.Adapters; // Introduce Wrapper
using Volo.Abp;
using Casbin.Adapter.SqlSugar.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;
using Casbin.Persist;


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

            // 1. 注册 Adapter 包装器 (它本身无状态，Scoped/Singleton 均可，但它依赖 ScopeFactory 是单例安全的)
            context.Services.AddSingleton<IAdapter, ScopeFactoryCasbinAdapter>();

            // 2. 注册 Casbin Enforcer 为 Singleton (机器缓存核心)
            context.Services.AddSingleton<IEnforcer>(sp =>
            {
                var adapter = sp.GetRequiredService<IAdapter>();
                var modelPath = Path.Combine(AppContext.BaseDirectory, "rbac_with_domains_model.conf");
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Casbin model file not found at: {modelPath}");
                }
                
                // 使用适配器初始化
                var enforcer = new Enforcer(modelPath, adapter);
                
                // 3. 关键：禁用自动保存！！！！
                // 因为写入操作由 Repository 接管，Enforcer 只负责读
                enforcer.EnableAutoSave(false);
                
                // 4. 首次全量加载
                // 内部会调用 adapter.LoadPolicy，我们的 adapter 会创建 Scope 读取数据库
                enforcer.LoadPolicy();
                
                return enforcer;
            });
        }
        public override async Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var db = context.ServiceProvider.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient;
            // 确保 CasbinRule 表存在
            db.CodeFirst.InitTables(typeof(CasbinRule));
            await base.OnPostApplicationInitializationAsync(context);
        }
    }
}
