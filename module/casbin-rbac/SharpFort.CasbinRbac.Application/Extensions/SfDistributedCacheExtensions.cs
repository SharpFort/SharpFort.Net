using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SharpFort.CasbinRbac.Application.Extensions;

/// <summary>
/// IDistributedCache 扩展方法，提供 JSON 序列化/反序列化的缓存读写。
/// MenuService / UserService / RoleService 统一使用。
/// </summary>
public static class SfDistributedCacheExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>默认缓存 TTL：30 分钟</summary>
    public static readonly DistributedCacheEntryOptions DefaultCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    /// <summary>短 TTL：1 分钟，用于空结果防穿透</summary>
    public static readonly DistributedCacheEntryOptions ShortCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    /// <summary>从缓存反序列化对象</summary>
    public static async Task<T?> GetFromCacheAsync<T>(this IDistributedCache cache, string key)
    {
        string? json = await cache.GetStringAsync(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>将对象序列化写入缓存</summary>
    public static async Task SetCacheAsync<T>(
        this IDistributedCache cache, string key, T value,
        DistributedCacheEntryOptions? options = null)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, json, options ?? DefaultCacheOptions);
    }
}
