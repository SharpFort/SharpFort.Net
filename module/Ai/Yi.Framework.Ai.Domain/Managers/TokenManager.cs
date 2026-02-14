using SqlSugar;
using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.Entities;
// using Yi.Framework.Ai.Domain.Entities; // Merged into Entities
// using Yi.Framework.Ai.Domain.Entities; // Token is now in Entities
using Yi.Framework.Ai.Domain.Shared.Consts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

/// <summary>
/// Token验证结果
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// 用户Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Token Id
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Token名称
    /// </summary>
    public string TokenName { get; set; }

    /// <summary>
    /// 是否启用请求日志记录
    /// </summary>
    public bool IsEnableLog { get; set; }
}

public class TokenManager : DomainService
{
    private readonly ISqlSugarRepository<Token> _tokenRepository;
    private readonly ISqlSugarRepository<AiUsage> _usageStatisticsRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _aiModelRepository;

    public TokenManager(
        ISqlSugarRepository<Token> tokenRepository,
        ISqlSugarRepository<AiUsage> usageStatisticsRepository,
        ISqlSugarRepository<AiModel, Guid> aiModelRepository)
    {
        _tokenRepository = tokenRepository;
        _usageStatisticsRepository = usageStatisticsRepository;
        _aiModelRepository = aiModelRepository;
    }

    /// <summary>
    /// 验证Token并返回用户Id和TokenId
    /// </summary>
    /// <param name="tokenOrId">Token密钥或者TokenId</param>
    /// <param name="modelId">模型Id（用于判断是否是尊享模型需要检查额度）</param>
    /// <returns>Token验证结果</returns>
    public async Task<TokenValidationResult> ValidateTokenAsync(object tokenOrId, string? modelId = null)
    {
        
        if (tokenOrId is null)
        {
            throw new UserFriendlyException("当前请求未包含token", "401");
        }
        
        Token entity;
        if (tokenOrId is Guid tokenId)
        {
             entity = await _tokenRepository._DbQueryable
                .Where(x => x.Id == tokenId)
                .FirstAsync();
        }
        else
        {
            var tokenStr = tokenOrId.ToString();
            if (!tokenStr.StartsWith("yi-"))
            {
                throw new UserFriendlyException("当前请求token非法", "401");
            }
            entity = await _tokenRepository._DbQueryable
                .Where(x => x.TokenKey == tokenStr)
                .FirstAsync();
        }
        
        if (entity is null)
        {
            throw new UserFriendlyException("当前请求token无效", "401");
        }

        // 检查Token是否被禁用
        if (entity.IsDisabled)
        {
            throw new UserFriendlyException("当前Token已被禁用，请启用后再使用", "403");
        }

        // 检查Token是否过期
        if (entity.ExpireTime.HasValue && entity.ExpireTime.Value < DateTime.Now)
        {
            throw new UserFriendlyException("当前Token已过期，请更新过期时间或创建新的Token", "403");
        }

        // 如果是尊享模型且Token设置了额度限制，检查是否超限
        if (!string.IsNullOrEmpty(modelId) && entity.PremiumQuotaLimit.HasValue)
        {
            var isPremium = await _aiModelRepository._DbQueryable
                .Where(x => x.ModelId == modelId)
                .Select(x => x.IsPremium)
                .FirstAsync();

            if (isPremium)
            {
                var usedQuota = await GetTokenPremiumUsedQuotaAsync(entity.UserId, entity.Id);
                if (usedQuota >= entity.PremiumQuotaLimit.Value)
                {
                    throw new UserFriendlyException($"当前Token的尊享包额度已用完（已使用：{usedQuota}，限制：{entity.PremiumQuotaLimit.Value}），请调整额度限制或使用其他Token", "403");
                }
            }
        }

        return new TokenValidationResult
        {
            UserId = entity.UserId,
            TokenId = entity.Id,
            Token = entity.TokenKey,
            TokenName = entity.Name,
            IsEnableLog = entity.IsEnableLog
        };
    }

    /// <summary>
    /// 获取Token的尊享包已使用额度
    /// </summary>
    private async Task<long> GetTokenPremiumUsedQuotaAsync(Guid userId, Guid tokenId)
    {
        // 先获取所有尊享模型的ModelId列表
        var premiumModelIds = await _aiModelRepository._DbQueryable
            .Where(x => x.IsPremium)
            .Select(x => x.ModelId)
            .ToListAsync();

        var usedQuota = await _usageStatisticsRepository._DbQueryable
            .Where(x => x.UserId == userId && x.TokenId == tokenId && premiumModelIds.Contains(x.ModelId))
            .SumAsync(x => x.TotalTokenCount);

        return usedQuota;
    }

    /// <summary>
    /// 获取用户的Token（兼容旧接口，返回第一个可用的Token）
    /// </summary>
    [Obsolete("请使用 ValidateTokenAsync 方法")]
    public async Task<string?> GetAsync(Guid userId)
    {
        var entity = await _tokenRepository._DbQueryable
            .Where(x => x.UserId == userId && !x.IsDisabled)
            .OrderBy(x => x.CreationTime)
            .FirstAsync();

        return entity?.TokenKey;
    }

    /// <summary>
    /// 获取用户Id（兼容旧接口）
    /// </summary>
    [Obsolete("请使用 ValidateTokenAsync 方法")]
    public async Task<Guid> GetUserIdAsync(string? token)
    {
        var result = await ValidateTokenAsync(token);
        return result.UserId;
    }
}
