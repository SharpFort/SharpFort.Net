namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 模型Token使用量统计DTO
/// </summary>
public class ModelTokenUsageDto
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string Model { get; set; }
    
    /// <summary>
    /// 总消耗量
    /// </summary>
    public long Tokens { get; set; }
    
    /// <summary>
    /// 占比（百分比）
    /// </summary>
    public decimal Percentage { get; set; }
}
