namespace Yi.Framework.Ai.Application.Contracts.Dtos.SystemStatistics;

/// <summary>
/// 利润统计输出
/// </summary>
public class ProfitStatisticsOutput
{
    /// <summary>
    /// 日期
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// 尊享包已消耗Token数(单位:个)
    /// </summary>
    public long TotalUsedTokens { get; set; }

    /// <summary>
    /// 尊享包已消耗Token数(单位:亿)
    /// </summary>
    public decimal TotalUsedTokensInHundredMillion { get; set; }

    /// <summary>
    /// 尊享包剩余库存Token数(单位:个)
    /// </summary>
    public long TotalRemainingTokens { get; set; }

    /// <summary>
    /// 尊享包剩余库存Token数(单位:亿)
    /// </summary>
    public decimal TotalRemainingTokensInHundredMillion { get; set; }

    /// <summary>
    /// 当前成本(RMB)
    /// </summary>
    public decimal CurrentCost { get; set; }

    /// <summary>
    /// 1亿Token成本(RMB)
    /// </summary>
    public decimal CostPerHundredMillion { get; set; }

    /// <summary>
    /// 总成本(RMB)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 总收益(RMB)
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// 利润率(%)
    /// </summary>
    public decimal ProfitRate { get; set; }

    /// <summary>
    /// 按200售价计算的成本(RMB)
    /// </summary>
    public decimal CostAt200Price { get; set; }
}
