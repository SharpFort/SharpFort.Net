using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace SharpFort.SettingManagement.Domain;

public class GlobalSettingManagementProvider(ISettingManagementStore settingManagementStore) : SettingManagementProvider(settingManagementStore), ITransientDependency
{
    public override string Name => GlobalSettingValueProvider.ProviderName;

    protected override string? NormalizeProviderKey(string? providerKey)
    {
        return null;
    }
}
