using Volo.Abp.Modularity;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Domain.Shared;

namespace SharpFort.CasbinRbac.Application.Contracts
{
    [DependsOn(
        typeof(SharpFortCasbinRbacDomainSharedModule),


        typeof(SharpFortDddApplicationContractsModule))]
    public class SharpFortCasbinRbacApplicationContractsModule : AbpModule
    {

    }
}