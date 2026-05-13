using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace SharpFort.SettingManagement.Domain;

public class TenantSettingManagementProvider(
    ISettingManagementStore settingManagementStore,
    ICurrentTenant currentTenant) : SettingManagementProvider(settingManagementStore), ITransientDependency
{
    public override string Name => TenantSettingValueProvider.ProviderName;

    protected ICurrentTenant CurrentTenant { get; } = currentTenant;

    protected override string? NormalizeProviderKey(string? providerKey)
    {
        return providerKey ?? (CurrentTenant.Id?.ToString());
    }
}
