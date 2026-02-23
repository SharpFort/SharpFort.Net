using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.Timing;
using SharpFort.SettingManagement.Domain;

namespace SharpFort.SettingManagement.Application;

[DependsOn(
    typeof(AbpDddApplicationModule),
    typeof(AbpSettingManagementApplicationContractsModule),
    typeof(SharpFortSettingManagementDomainModule),
    typeof(AbpTimingModule)

)]
public class SharpFortSettingManagementApplicationModule : AbpModule
{
}
