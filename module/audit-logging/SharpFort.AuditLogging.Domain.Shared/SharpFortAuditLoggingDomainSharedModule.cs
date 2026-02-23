using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace SharpFort.AuditLogging.Domain.Shared
{
    [DependsOn(typeof(AbpDddDomainSharedModule))]
    public class SharpFortAuditLoggingDomainSharedModule:AbpModule
    {

    }
}
