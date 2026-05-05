using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Text.Formatting;

namespace SharpFort.SettingManagement.Domain;

[Serializable]
[IgnoreMultiTenancy]
public class SettingCacheItem
{
    private const string CacheKeyFormatString = "pn:{0},pk:{1},n:{2}";
    private static readonly CompositeFormat CacheKeyFormat = CompositeFormat.Parse(CacheKeyFormatString);

    public string? Value { get; set; }

    public SettingCacheItem()
    {

    }

    public SettingCacheItem(string? value)
    {
        Value = value;
    }

    public static string CalculateCacheKey(string name, string providerName, string providerKey)
    {
        return string.Format(CultureInfo.InvariantCulture, CacheKeyFormat, providerName, providerKey, name);
    }

    public static string? GetSettingNameFormCacheKeyOrNull(string cacheKey)
    {
        var result = FormattedStringValueExtracter.Extract(cacheKey, CacheKeyFormatString, true);
        return result.IsMatch ? result.Matches.Last().Value : null;
    }
}
