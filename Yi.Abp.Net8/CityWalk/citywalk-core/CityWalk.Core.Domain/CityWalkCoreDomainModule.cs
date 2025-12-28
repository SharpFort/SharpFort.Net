using Volo.Abp.Caching;
using Volo.Abp.Domain;
using CityWalk.Core.Domain.Shared;
using Yi.Framework.Mapster;

namespace CityWalk.Core.Domain
{
    [DependsOn(
        typeof(CityWalkCoreDomainSharedModule),

        typeof(YiFrameworkMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule)
    )]
    public class CityWalkCoreDomainModule : AbpModule
    {

    }
}