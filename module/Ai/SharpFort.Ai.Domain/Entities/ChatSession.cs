using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// 聊天会话
/// </summary>
[SugarTable("Ai_Session")]
[SugarIndex($"index_{{table}}_{nameof(UserId)}", nameof(UserId), OrderByType.Asc)]
public class ChatSession : FullAuditedAggregateRoot<Guid>
{
    public ChatSession()
    {
    }

    public Guid UserId { get; set; }
    public string SessionTitle { get; set; } = null!;

    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string SessionContent { get; set; } = null!;

    public string? Remark { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Chat;

    /// <summary>
    /// 绑定AI应用ID
    /// </summary>
    public Guid? AppId { get; set; }

    /// <summary>
    /// 最后一条消息摘要
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? LastMessage { get; set; }
}
