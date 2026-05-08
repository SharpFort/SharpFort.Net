using Volo.Abp.Auditing;
using Volo.Abp.Domain;
using SharpFort.AuditLogging.Domain.Shared;

namespace SharpFort.AuditLogging.Domain
{
    [DependsOn(typeof(SharpFortAuditLoggingDomainSharedModule),


        typeof(AbpDddDomainModule),
        typeof(AbpAuditingModule)
        )]
    public class SharpFortAuditLoggingDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

        }
    }
}
