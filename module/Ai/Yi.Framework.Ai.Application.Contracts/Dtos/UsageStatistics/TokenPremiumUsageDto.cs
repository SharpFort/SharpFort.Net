namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 尊享包不同Token用量占比DTO（饼图）
/// </summary>
public class TokenPremiumUsageDto
{
    /// <summary>
    /// Token Id
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Token名称
    /// </summary>
    public string TokenName { get; set; }

    /// <summary>
    /// Token消耗量
    /// </summary>
    public long Tokens { get; set; }

    /// <summary>
    /// 占比（百分比）
    /// </summary>
    public decimal Percentage { get; set; }
}
