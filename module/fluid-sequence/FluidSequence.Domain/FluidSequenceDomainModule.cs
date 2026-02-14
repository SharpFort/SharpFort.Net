using Volo.Abp.Caching;
using Volo.Abp.Domain;
using FluidSequence.Domain.Shared;
using Yi.Framework.Mapster;

namespace FluidSequence.Domain
{
    [DependsOn(
        typeof(FluidSequenceDomainSharedModule),

        typeof(YiFrameworkMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule)
    )]
    public class FluidSequenceDomainModule : AbpModule
    {

    }
}