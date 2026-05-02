namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.MonitorCache;

public class MonitorCacheGetListOutputDto
{
    public required string CacheName { get; set; }
    public required string CacheKey { get; set; }
    public required string CacheValue { get; set; }
    public string? Remark { get; set; }
}
