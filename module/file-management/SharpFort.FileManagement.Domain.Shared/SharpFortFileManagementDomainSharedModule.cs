using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace SharpFort.FileManagement.Domain.Shared
{
    [DependsOn(
        
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class SharpFortFileManagementDomainSharedModule : AbpModule
    {

    }
}