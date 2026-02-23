using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace SharpFort.Ai.Domain.Shared
{
    [DependsOn(
        
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class SharpFortAiDomainSharedModule : AbpModule
    {

    }
}
