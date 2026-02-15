using Volo.Abp.SettingManagement;
using Yi.Abp.Domain.Shared;
using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.TenantManagement.Application.Contracts;
using Yi.Framework.CasbinRbac.Application.Contracts;
using Yi.Framework.Ai.Application.Contracts;

namespace Yi.Abp.Application.Contracts
{
    [DependsOn(
        typeof(YiAbpDomainSharedModule),
        typeof(AbpSettingManagementApplicationContractsModule),
        typeof(YiFrameworkTenantManagementApplicationContractsModule),
        typeof(YiFrameworkDddApplicationContractsModule),
        typeof(YiFrameworkCasbinRbacApplicationContractsModule),
        typeof(YiFrameworkAiApplicationContractsModule)
        )]
    public class YiAbpApplicationContractsModule:AbpModule
    {

    }
}