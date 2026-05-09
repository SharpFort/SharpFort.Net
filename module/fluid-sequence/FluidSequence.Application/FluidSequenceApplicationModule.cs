using SharpFort.FluidSequence.Application.Contracts;
using SharpFort.FluidSequence.Domain;
using SharpFort.Ddd.Application;

namespace SharpFort.FluidSequence.Application
{
    [DependsOn(
        typeof(FluidSequenceApplicationContractsModule),
        typeof(FluidSequenceDomainModule),

        typeof(SharpFortDddApplicationModule)

    )]
    public class FluidSequenceApplicationModule : AbpModule
    {
    }
}