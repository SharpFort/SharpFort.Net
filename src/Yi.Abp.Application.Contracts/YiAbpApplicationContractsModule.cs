using Volo.Abp.SettingManagement;
using Yi.Abp.Domain.Shared;
//using Yi.Framework.Bbs.Application.Contracts;
//using Yi.Framework.ChatHub.Application.Contracts;
using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.Rbac.Application.Contracts;
using Yi.Framework.TenantManagement.Application.Contracts;
//using CityWalk.Core.Application.Contracts;
using Yi.Framework.CasbinRbac.Application.Contracts;
using Yi.Framework.Ai.Application.Contracts;

namespace Yi.Abp.Application.Contracts
{
    [DependsOn(
        typeof(YiAbpDomainSharedModule),

        typeof(YiFrameworkRbacApplicationContractsModule),
        //typeof(YiFrameworkBbsApplicationContractsModule),
        //typeof(YiFrameworkChatHubApplicationContractsModule),
        typeof(AbpSettingManagementApplicationContractsModule),

        typeof(YiFrameworkTenantManagementApplicationContractsModule),
        typeof(YiFrameworkDddApplicationContractsModule),
        //typeof(CityWalkCoreApplicationContractsModule),
        typeof(YiFrameworkCasbinRbacApplicationContractsModule),
        typeof(YiFrameworkAiApplicationContractsModule)
        )]
    public class YiAbpApplicationContractsModule:AbpModule
    {

    }
}