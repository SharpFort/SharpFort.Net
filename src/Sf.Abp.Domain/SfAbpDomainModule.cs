using Volo.Abp.Caching;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Sf.Abp.Domain.Shared;
using SharpFort.AuditLogging.Domain;
using SharpFort.Mapster;
using SharpFort.SettingManagement.Domain;
using SharpFort.TenantManagement.Domain;
using SharpFort.CasbinRbac.Domain;
using SharpFort.Ai.Domain;

namespace Sf.Abp.Domain
{
    [DependsOn(
        typeof(SfAbpDomainSharedModule),
        typeof(SharpFortTenantManagementDomainModule),
        typeof(SharpFortAuditLoggingDomainModule),
        typeof(SharpFortSettingManagementDomainModule),
        typeof(SharpFortMapsterModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(SharpFortCasbinRbacDomainModule),
        typeof(SharpFortAiDomainModule)
        )]
    public class SfAbpDomainModule : AbpModule
    {

    }
}