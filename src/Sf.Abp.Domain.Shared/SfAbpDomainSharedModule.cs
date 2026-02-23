using Volo.Abp.Domain;
using Volo.Abp.SettingManagement;
using SharpFort.AuditLogging.Domain.Shared;
using SharpFort.CasbinRbac.Domain.Shared;
using SharpFort.Ai.Domain.Shared;

namespace Sf.Abp.Domain.Shared
{
    [DependsOn(
        typeof(SharpFortAuditLoggingDomainSharedModule),
        typeof(AbpSettingManagementDomainSharedModule),
        typeof(AbpDddDomainSharedModule),
        typeof(SharpFortCasbinRbacDomainSharedModule),
        typeof(SharpFortAiDomainSharedModule)
        )]
    public class SfAbpDomainSharedModule : AbpModule
    {

    }
}