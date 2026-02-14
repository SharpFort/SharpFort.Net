using Volo.Abp.SettingManagement;
using Yi.Framework.Ai.Domain.Shared;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts
{
    [DependsOn(
        typeof(YiFrameworkAiDomainSharedModule),

        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(YiFrameworkDddApplicationContractsModule))]
    public class YiFrameworkAiApplicationContractsModule:AbpModule
    {

    }
}