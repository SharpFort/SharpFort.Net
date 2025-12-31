using System;
using System.IO;
using Casbin;
using Casbin.Adapter.SqlSugar;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Yi.Framework.Mapster;
using Yi.Framework.CasbinRbac.Domain;
using Yi.Framework.SqlSugarCore;

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

            // 注册 Casbin Adapter
            context.Services.AddScoped<IAdapter>(sp =>
            {
                var dbContext = sp.GetRequiredService<YiCasbinRbacDbContext>();
                // 确保 YiCasbinRbacDbContext 继承的 SqlSugarDbContext 暴露了 SqlSugarClient
                return new SqlSugarAdapter(dbContext.SqlSugarClient);
            });

            // 注册 Casbin Enforcer
            context.Services.AddScoped<IEnforcer>(sp =>
            {
                var adapter = sp.GetRequiredService<IAdapter>();
                var modelPath = Path.Combine(AppContext.BaseDirectory, "rbac_with_domains_model.conf");
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Casbin model file not found at: {modelPath}");
                }
                var enforcer = new Enforcer(modelPath, adapter);
                
                // 启用自动保存 (AddPolicy 时自动写入数据库)
                enforcer.EnableAutoSave(true);
                
                // 加载策略
                enforcer.LoadPolicy();
                
                return enforcer;
            });
        }
    }
}
