using System.Globalization;
using System.Text;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace SharpFort.TenantManagement.Domain;

[Serializable]
[IgnoreMultiTenancy]
public class TenantCacheItem
{
    private static readonly CompositeFormat CacheKeyFormat = CompositeFormat.Parse("i:{0},n:{1}");

    public TenantConfiguration Value { get; set; } = null!;

    public TenantCacheItem()
    {

    }

    public TenantCacheItem(TenantConfiguration value)
    {
        Value = value;
    }

    public static string CalculateCacheKey(Guid? id, string name)
    {
        if (id == null && name.IsNullOrWhiteSpace())
        {
            throw new AbpException("Both id and name can't be invalid.");
        }

        return string.Format(CultureInfo.InvariantCulture, CacheKeyFormat,
            id?.ToString() ?? "null",
            name.IsNullOrWhiteSpace() ? "null" : name);
    }
}
