using Yi.Framework.Ai.Application.Contracts;
using Yi.Framework.Ai.Domain;
using Yi.Framework.Ddd.Application;

namespace Yi.Framework.Ai.Application
{
    [DependsOn(
        typeof(YiFrameworkAiApplicationContractsModule),
        typeof(YiFrameworkAiDomainModule),
        
        typeof(YiFrameworkDddApplicationModule)

    )]
    public class YiFrameworkAiApplicationModule : AbpModule
    {
    }
}
