namespace Yi.Framework.Ai.Application.Contracts.Dtos.SystemStatistics;

/// <summary>
/// 模型Token统计DTO
/// </summary>
public class ModelTokenStatisticsDto
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; }

    /// <summary>
    /// Token消耗量
    /// </summary>
    public long Tokens { get; set; }

    /// <summary>
    /// Token消耗量(万)
    /// </summary>
    public decimal TokensInWan { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// 成本(RMB)
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// 1亿Token成本(RMB)
    /// </summary>
    public decimal CostPerHundredMillion { get; set; }
}
