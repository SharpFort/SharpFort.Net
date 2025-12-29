using FreeRedis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Yi.Framework.Bbs.Domain.Shared.Caches;
using Yi.Framework.Bbs.Domain.Shared.Etos;

namespace Yi.Framework.Bbs.Application.Extensions;

/// <summary>
/// 访问日志中间件
/// 并发最高，采用缓存，默认10分钟才会真正操作一次数据库
/// 需考虑一致性问题，又不能上锁影响性能
/// </summary>
public class BbsAccessLogMiddleware : IMiddleware, ITransientDependency
{
    private static int _BbsAccessLogNumber = 0;

    internal static void ResetBbsAccessLogNumber()
    {
        _BbsAccessLogNumber = 0;
    }
    internal static int GetBbsAccessLogNumber()
    {
        return _BbsAccessLogNumber;
    }
    
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);

        Interlocked.Increment(ref _BbsAccessLogNumber);
    }
}

public class BbsAccessLogResetEventHandler : ILocalEventHandler<BbsAccessLogResetArgs>,
    ITransientDependency
{
    /// <summary>
    /// 缓存前缀
    /// </summary>
    private string CacheKeyPrefix => LazyServiceProvider.LazyGetRequiredService<IOptions<AbpDistributedCacheOptions>>()
        .Value.KeyPrefix;

    /// <summary>
    /// 使用懒加载防止报错
    /// </summary>
    private IRedisClient RedisClient => LazyServiceProvider.LazyGetRequiredService<IRedisClient>();

    /// <summary>
    /// 属性注入
    /// </summary>
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }

    private bool EnableRedisCache
    {
        get
        {
            var redisEnabled = LazyServiceProvider.LazyGetRequiredService<IConfiguration>()["Redis:IsEnabled"];
            return redisEnabled.IsNullOrEmpty() || bool.Parse(redisEnabled);
        }
    }
    
    //该事件由job定时10秒触发
    public async Task HandleEventAsync(BbsAccessLogResetArgs eventData)
    {
        if (EnableRedisCache)
        {
            //分布式锁
            if (await RedisClient.SetNxAsync("BbsAccessLogLock",true,TimeSpan.FromSeconds(5)))
            {
                //自增长数
                var incrNumber= BbsAccessLogMiddleware.GetBbsAccessLogNumber();
                //立即重置，开始计算，方式丢失
                BbsAccessLogMiddleware.ResetBbsAccessLogNumber();
                if (incrNumber>0)
                {
                    await RedisClient.IncrByAsync(
                        $"{CacheKeyPrefix}{BbsAccessLogCacheConst.Key}:{DateTime.Now.Date:yyyyMMdd}", incrNumber);
                }
             
                
            }
         
        }
    }
}