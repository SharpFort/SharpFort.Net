using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;

namespace SharpFort.TenantManagement.Domain
{
    public static class TenantManagementExtensions
    {
        public static IDisposable ChangeDefalut(this ICurrentTenant currentTenant)
        {
            return currentTenant.Change(null, ConnectionStrings.DefaultConnectionStringName);
        }
    }
}
