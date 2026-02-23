using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using SharpFort.AuditLogging.Domain;
using SharpFort.AuditLogging.Domain.Repositories;
using SharpFort.AuditLogging.SqlSugarCore.Repositories;
using SharpFort.SqlSugarCore;

namespace SharpFort.AuditLogging.SqlSugarCore
{
    [DependsOn(
        typeof(SharpFortAuditLoggingDomainModule),

        typeof(SharpFortSqlSugarCoreModule))]
    public class SharpFortAuditLoggingSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IAuditLogRepository, SqlSugarCoreAuditLogRepository>();

        }
    }
}
