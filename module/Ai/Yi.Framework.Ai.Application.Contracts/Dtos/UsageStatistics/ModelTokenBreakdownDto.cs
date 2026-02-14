namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 模型Token堆叠数据DTO（用于柱状图）
/// </summary>
public class ModelTokenBreakdownDto
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// Token消耗量
    /// </summary>
    public long Tokens { get; set; }
}
