using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace Yi.Framework.FileManagement.Domain.Shared
{
    [DependsOn(
        
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class YiFrameworkFileManagementDomainSharedModule : AbpModule
    {

    }
}