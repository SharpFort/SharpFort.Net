using Volo.Abp.Application.Dtos;
using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;

public class ChatMessageDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Content { get; set; }
    public string Role { get; set; }
    public string ModelId { get; set; }
    public string Remark { get; set; }
    public MessageTypeEnum MessageType { get; set; }
}
