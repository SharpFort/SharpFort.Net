using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace SharpFort.TenantManagement.Domain;

[Dependency(ReplaceServices = true)]
public class SfMultiTenantConnectionStringResolver(
    IOptionsMonitor<AbpDbConnectionOptions> options,
    ICurrentTenant currentTenant,
    IServiceProvider serviceProvider) : DefaultConnectionStringResolver(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public override async Task<string> ResolveAsync(string? connectionStringName = null)
    {
        if (_currentTenant.Id == null)
        {
            //No current tenant, fallback to default logic
            return await base.ResolveAsync(connectionStringName);
        }

        TenantConfiguration? tenant = await FindTenantConfigurationAsync(_currentTenant.Id.Value);

        if (tenant == null || tenant.ConnectionStrings.IsNullOrEmpty())
        {
            //Tenant has not defined any connection string, fallback to default logic
            return await base.ResolveAsync(connectionStringName);
        }

        string? tenantDefaultConnectionString = tenant.ConnectionStrings?.Default;

        //Requesting default connection string...
        if (connectionStringName is null or
            ConnectionStrings.DefaultConnectionStringName)
        {
            //Return tenant's default or global default
            return !tenantDefaultConnectionString.IsNullOrWhiteSpace()
                ? tenantDefaultConnectionString!
                : Options.ConnectionStrings.Default!;
        }

        //Requesting specific connection string...
        string? connString = tenant.ConnectionStrings?.GetOrDefault(connectionStringName);
        if (!connString.IsNullOrWhiteSpace())
        {
            //Found for the tenant
            return connString!;
        }

        //Fallback to the mapped database for the specific connection string
        AbpDatabaseInfo? database = Options.Databases.GetMappedDatabaseOrNull(connectionStringName);
        if (database != null && database.IsUsedByTenants)
        {
            connString = tenant.ConnectionStrings?.GetOrDefault(database.DatabaseName);
            if (!connString.IsNullOrWhiteSpace())
            {
                //Found for the tenant
                return connString!;
            }
        }

        //Fallback to tenant's default connection string if available
        return !tenantDefaultConnectionString.IsNullOrWhiteSpace()
            ? tenantDefaultConnectionString!
            : await base.ResolveAsync(connectionStringName);
    }

    [Obsolete("Use ResolveAsync method.")]
    public override string Resolve(string? connectionStringName = null)
    {
        if (_currentTenant.Id == null)
        {
            //No current tenant, fallback to default logic
            return base.Resolve(connectionStringName);
        }

        TenantConfiguration? tenant = FindTenantConfiguration(_currentTenant.Id.Value);

        if (tenant == null || tenant.ConnectionStrings.IsNullOrEmpty())
        {
            //Tenant has not defined any connection string, fallback to default logic
            return base.Resolve(connectionStringName);
        }

        string? tenantDefaultConnectionString = tenant.ConnectionStrings?.Default;

        //Requesting default connection string...
        if (connectionStringName is null or
            ConnectionStrings.DefaultConnectionStringName)
        {
            //Return tenant's default or global default
            return !tenantDefaultConnectionString.IsNullOrWhiteSpace()
                ? tenantDefaultConnectionString!
                : Options.ConnectionStrings.Default!;
        }

        //Requesting specific connection string...
        string? connString = tenant.ConnectionStrings?.GetOrDefault(connectionStringName);
        if (!connString.IsNullOrWhiteSpace())
        {
            //Found for the tenant
            return connString!;
        }

        //Fallback to tenant's default connection string if available
        if (!tenantDefaultConnectionString.IsNullOrWhiteSpace())
        {
            return tenantDefaultConnectionString!;
        }

        //Try to find the specific connection string for given name
        string? connStringInOptions = Options.ConnectionStrings.GetOrDefault(connectionStringName);
        if (!connStringInOptions.IsNullOrWhiteSpace())
        {
            return connStringInOptions!;
        }

        //Fallback to the global default connection string
        string? defaultConnectionString = Options.ConnectionStrings.Default;
        return !defaultConnectionString.IsNullOrWhiteSpace()
            ? defaultConnectionString!
            : throw new AbpException("No connection string defined!");
    }

    protected virtual async Task<TenantConfiguration?> FindTenantConfigurationAsync(Guid tenantId)
    {
        using (IServiceScope serviceScope = _serviceProvider.CreateScope())
        {
            ITenantStore tenantStore = serviceScope
                .ServiceProvider
                .GetRequiredService<ITenantStore>();

            return await tenantStore.FindAsync(tenantId);
        }
    }

    [Obsolete("Use FindTenantConfigurationAsync method.")]
    protected virtual TenantConfiguration? FindTenantConfiguration(Guid tenantId)
    {
        using (IServiceScope serviceScope = _serviceProvider.CreateScope())
        {
            ITenantStore tenantStore = serviceScope
                .ServiceProvider
                .GetRequiredService<ITenantStore>();

            return tenantStore.Find(tenantId);
        }
    }
}
