namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

/// <summary>
/// 每日Token使用量统计DTO
/// </summary>
public class DailyTokenUsageDto
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Token消耗量
    /// </summary>
    public long Tokens { get; set; }
}
