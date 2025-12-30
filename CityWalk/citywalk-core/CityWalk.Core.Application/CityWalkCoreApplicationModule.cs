using CityWalk.Core.Application.Contracts;
using CityWalk.Core.Domain;
using Yi.Framework.Ddd.Application;

namespace CityWalk.Core.Application
{
    [DependsOn(
        typeof(CityWalkCoreApplicationContractsModule),
        typeof(CityWalkCoreDomainModule),
        
        typeof(YiFrameworkDddApplicationModule)


    )]
    public class CityWalkCoreApplicationModule : AbpModule
    {
    }
}