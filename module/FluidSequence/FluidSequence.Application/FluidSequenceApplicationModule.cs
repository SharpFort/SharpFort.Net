using FluidSequence.Application.Contracts;
using FluidSequence.Domain;
using Yi.Framework.Ddd.Application;

namespace FluidSequence.Application
{
    [DependsOn(
        typeof(FluidSequenceApplicationContractsModule),
        typeof(FluidSequenceDomainModule),
        
        typeof(YiFrameworkDddApplicationModule)

    )]
    public class FluidSequenceApplicationModule : AbpModule
    {
    }
}