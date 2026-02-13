using Volo.Abp.Modularity;
using FluidSequence.Domain;
using Yi.Framework.SqlSugarCore;

namespace FluidSequence.SqlSugarCore
{
    [DependsOn(
        typeof(FluidSequenceDomainModule),
        typeof(YiFrameworkSqlSugarCoreModule)
    )]
    public class FluidSequenceSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddYiDbContext<FluidSequenceDbContext>();
        }
    }
}