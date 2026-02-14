using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace Yi.Framework.Ai.Domain.Shared
{
    [DependsOn(
        
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class YiFrameworkAiDomainSharedModule : AbpModule
    {

    }
}
