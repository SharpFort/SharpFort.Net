using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Sf.Abp.Domain;
using SharpFort.SqlSugarCore;
using SharpFort.AuditLogging.SqlSugarCore;
using SharpFort.CodeGen.SqlSugarCore;
using SharpFort.Mapster;
using SharpFort.SettingManagement.SqlSugarCore;
using Sf.Abp.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.TenantManagement.SqlSugarCore;
using SharpFort.CasbinRbac.SqlSugarCore;
using SharpFort.FileManagement.SqlSugarCore;
using SharpFort.Ai.SqlSugarCore;
using FluidSequence.SqlSugarCore;


namespace Sf.Abp.SqlsugarCore
{
    [DependsOn(
        typeof(SfAbpDomainModule),
        typeof(SharpFortCodeGenSqlSugarCoreModule),
        typeof(SharpFortSettingManagementSqlSugarCoreModule),
        typeof(SharpFortAuditLoggingSqlSugarCoreModule),
        typeof(SharpFortTenantManagementSqlSugarCoreModule),
        typeof(SharpFortMapsterModule),
        typeof(SharpFortSqlSugarCoreModule),
        typeof(SharpFortCasbinRbacSqlSugarCoreModule),
        typeof(SharpFortFileManagementSqlSugarCoreModule),
        typeof(SharpFortAiSqlSugarCoreModule),
        typeof(FluidSequenceSqlSugarCoreModule)
    )]
    public class SfAbpSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSfDbContext<SfDbContext>();
            //默认不开放，可根据项目需要是否Db直接对外开放
            //context.Services.AddTransient(x => x.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient);
        }
    }
}