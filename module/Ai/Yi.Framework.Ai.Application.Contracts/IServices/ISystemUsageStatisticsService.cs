using Yi.Framework.Ai.Application.Contracts.Dtos.SystemStatistics;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// 系统使用量统计服务接口
/// </summary>
public interface ISystemUsageStatisticsService
{
    /// <summary>
    /// 获取利润统计数据
    /// </summary>
    Task<ProfitStatisticsOutput> GetProfitStatisticsAsync(ProfitStatisticsInput input);

    /// <summary>
    /// 获取指定日期各模型Token统计
    /// </summary>
    Task<TokenStatisticsOutput> GetTokenStatisticsAsync(TokenStatisticsInput input);
}
