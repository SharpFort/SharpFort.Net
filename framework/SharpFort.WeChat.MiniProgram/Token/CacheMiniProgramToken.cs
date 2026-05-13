using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;

namespace SharpFort.WeChat.MiniProgram.Token;

internal sealed class CacheMiniProgramToken(IOptions<WeChatMiniProgramOptions> options, IDistributedCache<string> cache) : DefaultMinProgramToken(options), IMiniProgramToken
{
    private readonly IDistributedCache<string> _cache = cache;

    public new async Task<string> GetTokenAsync()
    {
        return await _cache.GetOrAddAsync("MiniProgramToken", base.GetTokenAsync, () =>
        {
            return new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) - TimeSpan.FromMinutes(1)
            };
        }) ?? throw new InvalidOperationException("Failed to get or add MiniProgramToken to cache.");
    }
}