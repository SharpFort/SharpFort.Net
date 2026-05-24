using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI消息存储 - 框架级消息持久化，记录ThreadId、Role、时间戳
/// </summary>
[SugarTable("Ai_ChatMessageStore")]
[SugarIndex($"index_{{table}}_{nameof(Key)}", nameof(Key), OrderByType.Asc)]
[SugarIndex($"index_{{table}}_{nameof(ThreadId)}", nameof(ThreadId), OrderByType.Asc)]
[SugarIndex($"index_{{table}}_{nameof(Role)}", nameof(Role), OrderByType.Asc)]
[SugarIndex($"index_{{table}}_{nameof(MessageId)}", nameof(MessageId), OrderByType.Asc)]
public class AiChatMessageStore : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 存储标识Key
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? Key { get; set; }

    /// <summary>
    /// 对话线程ID
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? ThreadId { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// 消息角色 (user/assistant/system/tool)
    /// </summary>
    [SugarColumn(Length = 50)]
    public string? Role { get; set; }

    /// <summary>
    /// 序列化消息内容
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? SerializedMessage { get; set; }

    /// <summary>
    /// 消息文本（可读）
    /// </summary>
    public string? MessageText { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? MessageId { get; set; }
}
