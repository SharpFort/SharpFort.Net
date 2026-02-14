using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

public class AiRechargeManager : DomainService
{
    private readonly ISqlSugarRepository<AiRecharge> _rechargeRepository;
    private readonly ISqlSugarRepository<Token> _tokenRepository;
    private readonly ILogger<AiRechargeManager> _logger;

    public AiRechargeManager(ISqlSugarRepository<AiRecharge> rechargeRepository,
        ISqlSugarRepository<Token> tokenRepository, ILogger<AiRechargeManager> logger)
    {
        _rechargeRepository = rechargeRepository;
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    public async Task<List<Guid>?> RemoveVipByExpireAsync()
    {
        _logger.LogInformation("开始执行VIP过期自动卸载任务");

        // 获取当前时间
        var currentTime = DateTime.Now;

        // 查找所有充值记录，按用户分组
        var allRecharges = await _rechargeRepository._DbQueryable.Where(x => x.RechargeType == RechargeTypeEnum.Vip)
            .ToListAsync();

        if (!allRecharges.Any())
        {
            _logger.LogInformation("没有找到任何充值记录");
            return null;
        }

        // 按用户分组，找出真正过期的用户
        var expiredUserIds = allRecharges
            .GroupBy(x => x.UserId)
            .Where(group =>
            {
                // 如果用户有任何一个过期时间为空的记录，说明是永久VIP，不过期
                if (group.Any(x => !x.ExpireDateTime.HasValue))
                    return false;

                // 找到用户最大的过期时间
                var maxExpireTime = group.Max(x => x.ExpireDateTime);

                // 如果最大过期时间小于当前时间，说明用户已过期(比较日期，满足用户最后一天)
                return maxExpireTime.HasValue && maxExpireTime.Value.Date < currentTime.Date;
            })
            .Select(group => group.Key)
            .ToList();

        if (!expiredUserIds.Any())
        {
            _logger.LogInformation("没有找到过期的VIP用户");
            return null;
        }

        _logger.LogInformation($"找到 {expiredUserIds.Count} 个过期的VIP用户");


        // 删除过期用户的Token密钥
        var removedTokenCount = await _tokenRepository.DeleteAsync(x => expiredUserIds.Contains(x.UserId));

        _logger.LogInformation($"成功删除 {removedTokenCount} 个用户的Token密钥");
        _logger.LogInformation($"VIP过期自动卸载任务执行完成，共处理 {expiredUserIds.Count} 个过期用户");

        return expiredUserIds;
    }
}