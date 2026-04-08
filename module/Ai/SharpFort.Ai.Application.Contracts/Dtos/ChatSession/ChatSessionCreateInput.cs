using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Application.Contracts.Dtos.ChatSession;

public class ChatSessionCreateInput
{
    public string SessionTitle { get; set; }
    public string SessionContent { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Chat;
}
