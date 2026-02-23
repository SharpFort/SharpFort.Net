using SharpFort.Ai.Application.Contracts;
using SharpFort.Ai.Domain;
using SharpFort.Ddd.Application;

namespace SharpFort.Ai.Application
{
    [DependsOn(
        typeof(SharpFortAiApplicationContractsModule),
        typeof(SharpFortAiDomainModule),
        
        typeof(SharpFortDddApplicationModule)

    )]
    public class SharpFortAiApplicationModule : AbpModule
    {
    }
}
