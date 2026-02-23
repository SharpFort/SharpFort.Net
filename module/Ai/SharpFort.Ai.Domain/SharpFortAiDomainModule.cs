using Volo.Abp.Caching;
using Volo.Abp.Domain;
using SharpFort.Ai.Domain.Shared;
using SharpFort.Mapster;

namespace SharpFort.Ai.Domain
{
    [DependsOn(
        typeof(SharpFortAiDomainSharedModule),

        typeof(SharpFortMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule)
    )]
    public class SharpFortAiDomainModule : AbpModule
    {

    }
}
