using SharpFort.TenantManagement.Domain;

namespace SharpFort.TenantManagement.Application
{
    [DependsOn(typeof(SharpFortTenantManagementDomainModule))]
    public class SharpFortTenantManagementApplicationModule : AbpModule
    {

    }
}
