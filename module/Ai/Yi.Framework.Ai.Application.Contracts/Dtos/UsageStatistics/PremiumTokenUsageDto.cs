namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 尊享服务Token用量统计DTO
/// </summary>
public class PremiumTokenUsageDto
{
    /// <summary>
    /// 总Token数
    /// </summary>
    public long PremiumTotalTokens { get; set; }

    /// <summary>
    /// 已使用Token数
    /// </summary>
    public long PremiumUsedTokens { get; set; }

    /// <summary>
    /// 剩余Token数
    /// </summary>
    public long PremiumRemainingTokens { get; set; }
}
