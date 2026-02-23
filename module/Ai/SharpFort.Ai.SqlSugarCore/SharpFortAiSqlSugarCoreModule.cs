using Volo.Abp.Modularity;
using SharpFort.Ai.Domain;
using SharpFort.Mapster;
using SharpFort.SqlSugarCore;

namespace SharpFort.Ai.SqlSugarCore
{
    [DependsOn(
        typeof(SharpFortAiDomainModule),
        typeof(SharpFortMapsterModule),
        typeof(SharpFortSqlSugarCoreModule)
    )]
    public class SharpFortAiSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSfDbContext<AiModuleDbContext>();
            //默认不开放，可根据项目需要是否Db直接对外开放
            //context.Services.AddTransient(x => x.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient);
        }
    }
}