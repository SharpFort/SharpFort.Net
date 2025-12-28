using Volo.Abp.SettingManagement;
using CityWalk.Core.Domain.Shared;
using Yi.Framework.Ddd.Application.Contracts;

namespace CityWalk.Core.Application.Contracts
{
    [DependsOn(
        typeof(CityWalkCoreDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(YiFrameworkDddApplicationContractsModule))]
    public class CityWalkCoreApplicationContractsModule:AbpModule
    {

    }
}