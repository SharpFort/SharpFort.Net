using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;

namespace CityWalk.Core.Domain.Shared
{
    [DependsOn(
        
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule))]
    public class CityWalkCoreDomainSharedModule : AbpModule
    {

    }
}