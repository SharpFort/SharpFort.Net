using System.Threading.Tasks;
using Volo.Abp.Auditing;
using Yi.Framework.AuditLogging.Domain.Entities;

namespace Yi.Framework.AuditLogging.Domain;

public interface IAuditLogInfoToAuditLogConverter
{
    Task<AuditLog> ConvertAsync(AuditLogInfo auditLogInfo);
}
