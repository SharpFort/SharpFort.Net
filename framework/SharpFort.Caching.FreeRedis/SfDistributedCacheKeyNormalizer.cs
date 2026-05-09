using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace SharpFort.Caching.FreeRedis
{
    /// <summary>
    /// 缓存键标准化处理器
    /// 用于处理缓存键的格式化和多租户支持
    /// </summary>
    /// <remarks>
    /// 构造函数
    /// </remarks>
    /// <param name="currentTenant">当前租户服务</param>
    /// <param name="distributedCacheOptions">分布式缓存配置选项</param>
    [Dependency(ReplaceServices = true)]
    public class SfDistributedCacheKeyNormalizer(
        ICurrentTenant currentTenant,
        IOptions<AbpDistributedCacheOptions> distributedCacheOptions) : IDistributedCacheKeyNormalizer, ITransientDependency
    {
        private readonly ICurrentTenant _currentTenant = currentTenant;
        private readonly AbpDistributedCacheOptions _distributedCacheOptions = distributedCacheOptions.Value;

        /// <summary>
        /// 标准化缓存键
        /// </summary>
        /// <param name="args">缓存键标准化参数</param>
        /// <returns>标准化后的缓存键</returns>
        public virtual string NormalizeKey(DistributedCacheKeyNormalizeArgs args)
        {
            // 添加全局缓存前缀
            string normalizedKey = $"{_distributedCacheOptions.KeyPrefix}{args.Key}";

            //todo 多租户支持已注释，如需启用取消注释即可
            //if (!args.IgnoreMultiTenancy && _currentTenant.Id.HasValue)
            //{
            //    normalizedKey = $"t:{_currentTenant.Id.Value},{normalizedKey}";
            //}

            return normalizedKey;
        }
    }
}
