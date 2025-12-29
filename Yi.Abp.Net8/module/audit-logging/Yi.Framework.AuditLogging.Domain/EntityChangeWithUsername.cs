using Yi.Framework.AuditLogging.Domain.Entities;

namespace Yi.Framework.AuditLogging.Domain;

public class EntityChangeWithUsername
{
    public EntityChange EntityChange { get; set; }

    public string UserName { get; set; }
}
