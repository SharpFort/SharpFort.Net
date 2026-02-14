namespace Yi.Framework.Ai.Application.Contracts.Dtos.SystemStatistics;

/// <summary>
/// Token统计输出
/// </summary>
public class TokenStatisticsOutput
{
    /// <summary>
    /// 日期
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// 模型统计列表
    /// </summary>
    public List<ModelTokenStatisticsDto> ModelStatistics { get; set; } = new();
}
