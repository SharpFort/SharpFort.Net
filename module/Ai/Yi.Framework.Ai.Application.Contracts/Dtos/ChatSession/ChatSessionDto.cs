using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.ChatSession;

public class ChatSessionDto : FullAuditedEntityDto<Guid>
{
    public string SessionTitle { get; set; }
    public string SessionContent { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionTypeEnum SessionType { get; set; }
}
