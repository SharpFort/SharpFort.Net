using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.TenantManagement.Application.Contracts
{
    [DependsOn(typeof(AbpTenantManagementDomainSharedModule),
        typeof(SharpFortDddApplicationContractsModule))]
    public class SharpFortTenantManagementApplicationContractsModule:AbpModule
    {

    }
}
