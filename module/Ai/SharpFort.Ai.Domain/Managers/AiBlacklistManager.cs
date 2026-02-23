using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

public class AiBlacklistManager : DomainService
{
    private readonly ISqlSugarRepository<AiBlacklist> _aiBlacklistRepository;

    public AiBlacklistManager(ISqlSugarRepository<AiBlacklist> aiBlacklistRepository)
    {
        _aiBlacklistRepository = aiBlacklistRepository;
    }

    /// <summary>
    /// 校验黑名单
    /// </summary>
    /// <param name="userId"></param>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task VerifiyAiBlacklist(Guid userId)
    {
        var now = DateTime.Now;
        if (await _aiBlacklistRepository._DbQueryable
                .Where(x => now >= x.StartTime && now <= x.EndTime)
                .AnyAsync(x => x.UserId == userId))
        {
            throw new UserFriendlyException("当前用户已被加入黑名单,请联系管理员处理");
        }
    }
}
