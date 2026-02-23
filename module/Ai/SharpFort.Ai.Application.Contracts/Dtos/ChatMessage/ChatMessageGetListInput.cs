using System.ComponentModel.DataAnnotations;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;

public class ChatMessageGetListInput : PagedAllResultRequestDto
{
    [Required]
    public Guid SessionId { get; set; }
}
