using Volo.Abp.Modularity;
using Yi.Framework.Ai.Domain;
using Yi.Framework.Mapster;
using Yi.Framework.SqlSugarCore;

namespace Yi.Framework.Ai.SqlSugarCore
{
    [DependsOn(
        typeof(YiFrameworkAiDomainModule),
        typeof(YiFrameworkMapsterModule),
        typeof(YiFrameworkSqlSugarCoreModule)
    )]
    public class YiFrameworkAiSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddYiDbContext<AiModuleDbContext>();
            //默认不开放，可根据项目需要是否Db直接对外开放
            //context.Services.AddTransient(x => x.GetRequiredService<ISqlSugarDbContext>().SqlSugarClient);
        }
    }
}