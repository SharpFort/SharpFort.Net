using System.Threading.Tasks;
using Volo.Abp.Auditing;
using SharpFort.AuditLogging.Domain.Entities;

namespace SharpFort.AuditLogging.Domain;

public interface IAuditLogInfoToAuditLogConverter
{
    Task<AuditLog> ConvertAsync(AuditLogInfo auditLogInfo);
}
