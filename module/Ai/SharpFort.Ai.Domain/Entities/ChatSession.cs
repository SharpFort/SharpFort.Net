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
    public string SessionTitle { get; set; }

    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string SessionContent { get; set; }
    
    public string? Remark { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Chat;
}
