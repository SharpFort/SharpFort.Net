using Volo.Abp.Caching;
using Volo.Abp.Domain;
using Yi.Framework.Ai.Domain.Shared;
using Yi.Framework.Mapster;

namespace Yi.Framework.Ai.Domain
{
    [DependsOn(
        typeof(YiFrameworkAiDomainSharedModule),

        typeof(YiFrameworkMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule)
    )]
    public class YiFrameworkAiDomainModule : AbpModule
    {

    }
}
