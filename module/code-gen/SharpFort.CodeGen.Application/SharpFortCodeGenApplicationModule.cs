using SharpFort.CodeGen.Application.Contracts;
using SharpFort.CodeGen.Domain;
using SharpFort.Ddd.Application;

namespace SharpFort.CodeGen.Application
{
    [DependsOn(typeof(SharpFortCodeGenApplicationContractsModule),
        typeof(SharpFortCodeGenDomainModule),
        typeof(SharpFortDddApplicationModule))]
    public class SharpFortCodeGenApplicationModule : AbpModule
    {

    }
}
