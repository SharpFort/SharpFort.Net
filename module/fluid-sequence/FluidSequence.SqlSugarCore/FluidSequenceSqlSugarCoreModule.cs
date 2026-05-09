using SharpFort.FluidSequence.Domain;
using SharpFort.SqlSugarCore;

namespace SharpFort.FluidSequence.SqlSugarCore
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