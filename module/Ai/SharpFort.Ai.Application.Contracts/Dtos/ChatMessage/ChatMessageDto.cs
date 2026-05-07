using Volo.Abp.Application.Dtos;
using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;

public class ChatMessageDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Content { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ModelId { get; set; } = null!;
    public string Remark { get; set; } = null!;
    public MessageType MessageType { get; set; }
}
