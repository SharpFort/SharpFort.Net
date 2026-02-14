using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Yi.Abp.Domain;
using Yi.Abp.SqlSugarCore;
using Yi.Framework.AuditLogging.SqlSugarCore;
using Yi.Framework.CodeGen.SqlSugarCore;
using Yi.Framework.Mapster;
using Yi.Framework.Rbac.SqlSugarCore;
using Yi.Framework.SettingManagement.SqlSugarCore;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.SqlSugarCore.Abstractions;
using Yi.Framework.TenantManagement.SqlSugarCore;
using Yi.Framework.CasbinRbac.SqlSugarCore;
using Yi.Framework.FileManagement.SqlSugarCore;
using Yi.Framework.Ai.SqlSugarCore;
using FluidSequence.SqlSugarCore;


namespace Yi.Abp.SqlsugarCore
{
    [DependsOn(
        typeof(YiAbpDomainModule),
        typeof(YiFrameworkCodeGenSqlSugarCoreModule),
        typeof(YiFrameworkSettingManagementSqlSugarCoreModule),
        typeof(YiFrameworkAuditLoggingSqlSugarCoreModule),
        typeof(YiFrameworkTenantManagementSqlSugarCoreModule),
        typeof(YiFrameworkMapsterModule),
        typeof(YiFrameworkSqlSugarCoreModule),
        typeof(YiFrameworkCasbinRbacSqlSugarCoreModule),
        typeof(YiFrameworkFileManagementSqlSugarCoreModule),
        typeof(YiFrameworkAiSqlSugarCoreModule),
        typeof(FluidSequenceSqlSugarCoreModule)
    )]
    public class YiAbpSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddYiDbContext<YiDbContext>();
            //默认不开放，可根据项目需要是否Db直接对外开放
            //context.Services.AddTransient(x => x.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient);
        }
    }
}