using Volo.Abp.SettingManagement;
using Yi.Abp.Application.Contracts;
using Yi.Abp.Domain;
using Yi.Framework.Bbs.Application;
//using Yi.Framework.ChatHub.Application;
using Yi.Framework.CodeGen.Application;
using Yi.Framework.Ddd.Application;
//using Yi.Framework.DigitalCollectibles.Application;
using Yi.Framework.Rbac.Application;
using Yi.Framework.SettingManagement.Application;
//using Yi.Framework.Stock.Application;
using Yi.Framework.TenantManagement.Application;
using CityWalk.Core.Application;
using Yi.Framework.CasbinRbac.Application;

namespace Yi.Abp.Application
{
    [DependsOn(
        typeof(YiAbpApplicationContractsModule),
        typeof(YiAbpDomainModule),


        //typeof(YiFrameworkRbacApplicationModule),
        typeof(Yi.Framework.CasbinRbac.Application.YiFrameworkCasbinRbacApplicationModule),
        typeof(YiFrameworkBbsApplicationModule),
        //typeof(YiFrameworkDigitalCollectiblesApplicationModule),
        //typeof(YiFrameworkChatHubApplicationModule),
        //typeof(YiFrameworkStockApplicationModule),
        typeof(CityWalkCoreApplicationModule),


        typeof(YiFrameworkTenantManagementApplicationModule),
        typeof(YiFrameworkCodeGenApplicationModule),
        typeof (YiFrameworkSettingManagementApplicationModule),

        typeof(YiFrameworkDddApplicationModule),
        typeof(YiFrameworkCasbinRbacApplicationModule)
        
        )]
    public class YiAbpApplicationModule : AbpModule
    {
    }
}
