using Volo.Abp.Caching;
using Volo.Abp.Domain;
using FluidSequence.Domain.Shared;
using SharpFort.Mapster;

namespace FluidSequence.Domain
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