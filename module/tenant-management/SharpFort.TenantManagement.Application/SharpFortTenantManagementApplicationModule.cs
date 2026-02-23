using Volo.Abp.Modularity;
using SharpFort.Ddd.Application;
using SharpFort.TenantManagement.Domain;

namespace SharpFort.TenantManagement.Application
{
    [DependsOn(typeof(SharpFortTenantManagementDomainModule))]
    public class SharpFortTenantManagementApplicationModule: AbpModule
    {

    }
}
