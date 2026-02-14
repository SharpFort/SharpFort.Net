namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 每小时Token使用量统计DTO（柱状图）
/// </summary>
public class HourlyTokenUsageDto
{
    /// <summary>
    /// 小时时间点
    /// </summary>
    public DateTime Hour { get; set; }

    /// <summary>
    /// 该小时总Token消耗量
    /// </summary>
    public long TotalTokens { get; set; }

    /// <summary>
    /// 各模型Token消耗明细
    /// </summary>
    public List<ModelTokenBreakdownDto> ModelBreakdown { get; set; } = new();
}
