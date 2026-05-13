using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;
using Volo.Abp.Users;

namespace SharpFort.SettingManagement.Domain;

public class UserSettingManagementProvider(
    ISettingManagementStore settingManagementStore,
    ICurrentUser currentUser) : SettingManagementProvider(settingManagementStore), ITransientDependency
{
    public override string Name => UserSettingValueProvider.ProviderName;

    protected ICurrentUser CurrentUser { get; } = currentUser;

    protected override string? NormalizeProviderKey(string? providerKey)
    {
        return providerKey ?? (CurrentUser.Id?.ToString());
    }
}
