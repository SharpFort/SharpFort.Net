using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// Ai用量统计
/// </summary>
[SugarTable("Ai_Usage")]
[SugarIndex($"index_{{table}}_{nameof(UserId)}_{nameof(ModelId)}_{nameof(TokenId)}",
    nameof(UserId), OrderByType.Asc,
    nameof(ModelId), OrderByType.Asc,
    nameof(TokenId), OrderByType.Asc
)]
public class AiUsage : FullAuditedAggregateRoot<Guid>
{
    public AiUsage()
    {
    }

    public AiUsage(Guid? userId, string modelId, Guid tokenId)
    {
        UserId = userId;
        ModelId = modelId;
        TokenId = tokenId;
    }

    /// <summary>
    /// 用户id
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// Token密钥Id
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// 对话次数
    /// </summary>
    public int UsageTotalNumber { get; set; }

    /// <summary>
    /// 输出token总数
    /// </summary>
    public long UsageOutputTokenCount { get; set; }

    /// <summary>
    /// 输入token总数
    /// </summary>
    public long UsageInputTokenCount { get; set; }

    /// <summary>
    /// 总token数
    /// </summary>
    public long TotalTokenCount { get; set; }

    /// <summary>
    /// 新增一次聊天统计
    /// </summary>
    public void AddOnceChat(long inputTokenCount, long outputTokenCount)
    {
        UsageTotalNumber += 1;
        UsageOutputTokenCount += outputTokenCount;
        UsageInputTokenCount += inputTokenCount;
        TotalTokenCount += (outputTokenCount + inputTokenCount);
    }
}
