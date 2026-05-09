using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;

namespace SharpFort.WeChat.MiniProgram.Token;

internal class CacheMiniProgramToken(IOptions<WeChatMiniProgramOptions> options, IDistributedCache<string> cache) : DefaultMinProgramToken(options), IMiniProgramToken
{
    private IDistributedCache<string> _cache = cache;
    private const string CacheKey = "MiniProgramToken";

    public async Task<string> GetTokenAsync()
    {
        return await _cache.GetOrAddAsync("MiniProgramToken", async () => { return await base.GetTokenAsync(); }, () =>
        {
            return new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) - TimeSpan.FromMinutes(1)
            };
        });
    }
}