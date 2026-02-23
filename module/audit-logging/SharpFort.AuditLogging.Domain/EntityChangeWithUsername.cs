using SharpFort.AuditLogging.Domain.Entities;

namespace SharpFort.AuditLogging.Domain;

public class EntityChangeWithUsername
{
    public EntityChange EntityChange { get; set; }

    public string UserName { get; set; }
}
