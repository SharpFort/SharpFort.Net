using Volo.Abp.Modularity;
using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Domain.Shared;

namespace Yi.Framework.CasbinRbac.Application.Contracts
{
    [DependsOn(
        typeof(YiFrameworkCasbinRbacDomainSharedModule),


        typeof(YiFrameworkDddApplicationContractsModule))]
    public class YiFrameworkCasbinRbacApplicationContractsModule : AbpModule
    {

    }
}