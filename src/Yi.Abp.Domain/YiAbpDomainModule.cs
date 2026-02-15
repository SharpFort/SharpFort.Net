using Volo.Abp.Caching;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Yi.Abp.Domain.Shared;
using Yi.Framework.AuditLogging.Domain;
using Yi.Framework.Mapster;
using Yi.Framework.SettingManagement.Domain;
using Yi.Framework.TenantManagement.Domain;
using Yi.Framework.CasbinRbac.Domain;
using Yi.Framework.Ai.Domain;

namespace Yi.Abp.Domain
{
    [DependsOn(
        typeof(YiAbpDomainSharedModule),
        typeof(YiFrameworkTenantManagementDomainModule),
        typeof(YiFrameworkAuditLoggingDomainModule),
        typeof(YiFrameworkSettingManagementDomainModule),
        typeof(YiFrameworkMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(YiFrameworkCasbinRbacDomainModule),
        typeof(YiFrameworkAiDomainModule)
        )]
    public class YiAbpDomainModule : AbpModule
    {

    }
}