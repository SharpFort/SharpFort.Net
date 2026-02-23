using Volo.Abp.Collections;

namespace SharpFort.SettingManagement.Domain;

public class SettingManagementOptions
{
    public ITypeList<ISettingManagementProvider> Providers { get; }

    public SettingManagementOptions()
    {
        Providers = new TypeList<ISettingManagementProvider>();
    }
}
