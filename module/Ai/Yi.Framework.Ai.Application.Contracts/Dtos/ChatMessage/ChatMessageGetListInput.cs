using System.ComponentModel.DataAnnotations;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.ChatMessage;

public class ChatMessageGetListInput : PagedAllResultRequestDto
{
    [Required]
    public Guid SessionId { get; set; }
}
