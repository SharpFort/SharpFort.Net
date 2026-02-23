using Volo.Abp.Modularity;
using SharpFort.TenantManagement.Domain;

namespace SharpFort.TenantManagement.SqlSugarCore
{
    [DependsOn(typeof(SharpFortTenantManagementDomainModule))]
    public class SharpFortTenantManagementSqlSugarCoreModule : AbpModule
    {
    }
}
