namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 模型今日使用量统计DTO（卡片列表）
/// </summary>
public class ModelTodayUsageDto
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 今日使用次数
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 今日消耗总Token数
    /// </summary>
    public long TotalTokens { get; set; }
    
    /// <summary>
    /// 模型图标URL
    /// </summary>
    public string? IconUrl { get; set; }
}
