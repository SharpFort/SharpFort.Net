using SharpFort.CodeGen.Domain.Shared;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CodeGen.Application.Contracts
{
    [DependsOn(typeof(SharpFortCodeGenDomainSharedModule),
        typeof(SharpFortDddApplicationContractsModule))]
    public class SharpFortCodeGenApplicationContractsModule : AbpModule
    {

    }
}
