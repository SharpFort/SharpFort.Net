using Volo.Abp.SettingManagement;
using Yi.Framework.FileManagement.Domain.Shared;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.FileManagement.Application.Contracts
{
    [DependsOn(
        typeof(YiFrameworkFileManagementDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(YiFrameworkDddApplicationContractsModule))]
    public class YiFrameworkFileManagementApplicationContractsModule:AbpModule
    {

    }
}