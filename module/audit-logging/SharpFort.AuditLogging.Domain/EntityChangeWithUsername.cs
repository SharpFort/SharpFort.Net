using SharpFort.AuditLogging.Domain.Entities;

namespace SharpFort.AuditLogging.Domain;

public class EntityChangeWithUsername
{
    public required EntityChange EntityChange { get; set; }

    public required string UserName { get; set; }
}
