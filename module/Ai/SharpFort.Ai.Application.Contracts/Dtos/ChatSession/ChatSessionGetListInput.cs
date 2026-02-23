using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.Ai.Application.Contracts.Dtos.ChatSession;

public class ChatSessionGetListInput : PagedAllResultRequestDto
{
    public string? SessionTitle { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionTypeEnum? SessionType { get; set; }
}
