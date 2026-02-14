using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

/// <summary>
/// 模型管理器
/// </summary>
public class ModelManager : DomainService
{
    public readonly ISqlSugarRepository<AiModel> _aiModelRepository;
    private readonly IDistributedCache<List<string>, string> _distributedCache;
    private readonly ILogger<ModelManager> _logger;
    private const string PREMIUM_MODEL_IDS_CACHE_KEY = "PremiumModelIds";

    public ModelManager(
        ISqlSugarRepository<AiModel> aiModelRepository,
        IDistributedCache<List<string>, string> distributedCache,
        ILogger<ModelManager> logger)
    {
        _aiModelRepository = aiModelRepository;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有尊享模型ID列表(使用分布式缓存,10分钟过期)
    /// </summary>
    /// <returns>尊享模型ID列表</returns>
    public async Task<List<string>> GetPremiumModelIdsAsync()
    {
        var output = await _distributedCache.GetOrAddAsync(
            PREMIUM_MODEL_IDS_CACHE_KEY,
            async () =>
            {
                // 从数据库查询
                var premiumModelIds = await _aiModelRepository._DbQueryable
                    .Where(x => x.IsPremium && x.IsEnabled)
                    .Select(x => x.ModelId)
                    .ToListAsync();
                return premiumModelIds;
            },
            () => new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }
        );
        return output ?? new List<string>();
    }

    /// <summary>
    /// 判断指定模型是否为尊享模型
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <returns>是否为尊享模型</returns>
    public async Task<bool> IsPremiumModelAsync(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var premiumModelIds = await GetPremiumModelIdsAsync();
        return premiumModelIds.Contains(modelId);
    }

    /// <summary>
    /// 清除尊享模型ID缓存
    /// </summary>
    public async Task ClearPremiumModelIdsCacheAsync()
    {
        await _distributedCache.RemoveAsync(PREMIUM_MODEL_IDS_CACHE_KEY);
        _logger.LogInformation("已清除尊享模型ID分布式缓存");
    }
}
