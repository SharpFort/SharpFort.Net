using Volo.Abp.Domain;

namespace SharpFort.AuditLogging.Domain.Shared
{
    [DependsOn(typeof(AbpDddDomainSharedModule))]
    public class SharpFortAuditLoggingDomainSharedModule : AbpModule
    {

    }
}
