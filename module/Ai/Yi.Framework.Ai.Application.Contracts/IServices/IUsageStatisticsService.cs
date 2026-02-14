using Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// 使用量统计服务接口
/// </summary>
public interface IUsageStatisticsService
{
    /// <summary>
    /// 获取当前用户近7天的Token消耗统计
    /// </summary>
    /// <returns>每日Token使用量列表</returns>
    Task<List<DailyTokenUsageDto>> GetLast7DaysTokenUsageAsync(UsageStatisticsGetInput input);
    
    /// <summary>
    /// 获取当前用户各个模型的Token消耗量及占比
    /// </summary>
    /// <returns>模型Token使用量列表</returns>
    Task<List<ModelTokenUsageDto>> GetModelTokenUsageAsync(UsageStatisticsGetInput input);

    /// <summary>
    /// 获取当前用户近24小时每小时Token消耗统计（柱状图）
    /// </summary>
    /// <returns>每小时Token使用量列表，包含各模型堆叠数据</returns>
    Task<List<HourlyTokenUsageDto>> GetLast24HoursTokenUsageAsync(UsageStatisticsGetInput input);

    /// <summary>
    /// 获取当前用户今日各模型使用量统计（卡片列表）
    /// </summary>
    /// <returns>模型今日使用量列表，包含使用次数和总Token</returns>
    Task<List<ModelTodayUsageDto>> GetTodayModelUsageAsync(UsageStatisticsGetInput input);
}
