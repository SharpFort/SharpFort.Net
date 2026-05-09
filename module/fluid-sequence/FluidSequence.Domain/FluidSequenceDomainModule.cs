using Volo.Abp.Caching;
using Volo.Abp.Domain;
using SharpFort.FluidSequence.Domain.Shared;
using SharpFort.Mapster;

namespace SharpFort.FluidSequence.Domain
{
    [DependsOn(
        typeof(FluidSequenceDomainSharedModule),

        typeof(SharpFortMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule)
    )]
    public class FluidSequenceDomainModule : AbpModule
    {

    }
}