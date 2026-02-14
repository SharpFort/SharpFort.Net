using Volo.Abp.Application.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class MessageDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Content { get; set; }
    public string Role { get; set; }
    public string ModelId { get; set; }
    public string Remark { get; set; }
}
