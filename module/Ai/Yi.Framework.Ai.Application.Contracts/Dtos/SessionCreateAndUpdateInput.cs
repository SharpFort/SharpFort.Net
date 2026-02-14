using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class SessionCreateAndUpdateInput
{
    public string SessionTitle { get; set; }
    public string SessionContent { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionTypeEnum SessionType { get; set; } = SessionTypeEnum.Chat;
}
