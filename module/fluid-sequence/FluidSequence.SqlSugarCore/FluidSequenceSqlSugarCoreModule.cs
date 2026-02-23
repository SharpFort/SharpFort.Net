using Volo.Abp.Modularity;
using FluidSequence.Domain;
using SharpFort.SqlSugarCore;

namespace FluidSequence.SqlSugarCore
{
    [DependsOn(
        typeof(FluidSequenceDomainModule),
        typeof(SharpFortSqlSugarCoreModule)
    )]
    public class FluidSequenceSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSfDbContext<FluidSequenceDbContext>();
        }
    }
}