using Yi.Framework.Ai.Application.Contracts.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.Ranking;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// 排行榜服务接口
/// </summary>
public interface IRankingService
{
    /// <summary>
    /// 获取排行榜列表（全量返回）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>排行榜列表</returns>
    Task<List<RankingItemDto>> GetListAsync(RankingGetListInput input);
}
