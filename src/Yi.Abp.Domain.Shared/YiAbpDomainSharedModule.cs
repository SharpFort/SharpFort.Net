using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;
using Yi.Framework.AuditLogging.Domain.Shared;
using Yi.Framework.CasbinRbac.Domain.Shared;
using Yi.Framework.Ai.Domain.Shared;

namespace Yi.Abp.Domain.Shared
{
    [DependsOn(
        typeof(YiFrameworkAuditLoggingDomainSharedModule),
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule),
        typeof(YiFrameworkCasbinRbacDomainSharedModule),
        typeof(YiFrameworkAiDomainSharedModule)
        )]
    public class YiAbpDomainSharedModule : AbpModule
    {

    }
}