using System.Text.Json;

namespace Yi.Framework.Ai.Domain.Shared.Extensions;

public static class JsonElementExtensions
{
    #region 路径访问

    /// <summary>
    /// 链式获取深层属性，支持对象属性和数组索引
    /// </summary>
    /// <example>
    /// root.GetPath("user", "addresses", 0, "city")
    /// </example>
    public static JsonElement? GetPath(this JsonElement element, params object[] path)
    {
        JsonElement current = element;

        foreach (var key in path)
        {
            switch (key)
            {
                case string propertyName:
                    if (current.ValueKind != JsonValueKind.Object ||
                        !current.TryGetProperty(propertyName, out current))
                        return null;
                    break;

                case int index:
                    if (current.ValueKind != JsonValueKind.Array ||
                        index < 0 || index >= current.GetArrayLength())
                        return null;
                    current = current[index];
                    break;

                default:
                    return null;
            }
        }

        return current;
    }

    /// <summary>
    /// 安全获取对象属性
    /// </summary>
    public static JsonElement? Get(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
            return value;
        return null;
    }

    /// <summary>
    /// 安全获取数组元素
    /// </summary>
    public static JsonElement? Get(this JsonElement element, int index)
    {
        if (element.ValueKind == JsonValueKind.Array &&
            index >= 0 && index < element.GetArrayLength())
            return element[index];
        return null;
    }

    /// <summary>
    /// 链式安全获取对象属性
    /// </summary>
    public static JsonElement? Get(this JsonElement? element, string propertyName)
        => element?.Get(propertyName);

    /// <summary>
    /// 链式安全获取数组元素
    /// </summary>
    public static JsonElement? Get(this JsonElement? element, int index)
        => element?.Get(index);

    #endregion

    #region 取值方法（带默认值）

    public static string? GetString(this JsonElement? element, string? defaultValue = null)
        => element?.ValueKind == JsonValueKind.String ? element.Value.GetString() : defaultValue;

    public static int GetInt(this JsonElement? element, int defaultValue = 0)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetInt32() : defaultValue;

    public static long GetLong(this JsonElement? element, long defaultValue = 0)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetInt64() : defaultValue;

    public static double GetDouble(this JsonElement? element, double defaultValue = 0)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetDouble() : defaultValue;

    public static decimal GetDecimal(this JsonElement? element, decimal defaultValue = 0)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetDecimal() : defaultValue;

    public static bool GetBool(this JsonElement? element, bool defaultValue = false)
        => element?.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? element.Value.GetBoolean()
            : defaultValue;

    public static DateTime GetDateTime(this JsonElement? element, DateTime defaultValue = default)
        => element?.ValueKind == JsonValueKind.String && element.Value.TryGetDateTime(out var dt)
            ? dt
            : defaultValue;

    public static Guid GetGuid(this JsonElement? element, Guid defaultValue = default)
        => element?.ValueKind == JsonValueKind.String && element.Value.TryGetGuid(out var guid)
            ? guid
            : defaultValue;

    #endregion

    #region 可空取值方法

    public static int? GetIntOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetInt32() : null;

    public static long? GetLongOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetInt64() : null;

    public static double? GetDoubleOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetDouble() : null;

    public static decimal? GetDecimalOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Number ? element.Value.GetDecimal() : null;

    public static bool? GetBoolOrNull(this JsonElement? element)
        => element?.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? element.Value.GetBoolean()
            : null;

    public static DateTime? GetDateTimeOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.String && element.Value.TryGetDateTime(out var dt)
            ? dt
            : null;

    public static Guid? GetGuidOrNull(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.String && element.Value.TryGetGuid(out var guid)
            ? guid
            : null;

    #endregion

    #region 数组操作

    /// <summary>
    /// 安全获取数组，不存在返回空数组
    /// </summary>
    public static IEnumerable<JsonElement> GetArray(this JsonElement? element)
    {
        if (element?.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.Value.EnumerateArray())
                yield return item;
        }
    }

    /// <summary>
    /// 获取数组长度
    /// </summary>
    public static int GetArrayLength(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Array ? element.Value.GetArrayLength() : 0;

    /// <summary>
    /// 数组转 List
    /// </summary>
    public static List<string?> ToStringList(this JsonElement? element)
        => element.GetArray().Select(e => e.GetString()).ToList();

    public static List<int> ToIntList(this JsonElement? element)
        => element.GetArray()
            .Where(e => e.ValueKind == JsonValueKind.Number)
            .Select(e => e.GetInt32())
            .ToList();

    #endregion

    #region 对象操作

    /// <summary>
    /// 安全枚举对象属性
    /// </summary>
    public static IEnumerable<JsonProperty> GetProperties(this JsonElement? element)
    {
        if (element?.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.Value.EnumerateObject())
                yield return prop;
        }
    }

    /// <summary>
    /// 获取所有属性名
    /// </summary>
    public static IEnumerable<string> GetPropertyNames(this JsonElement? element)
        => element.GetProperties().Select(p => p.Name);

    /// <summary>
    /// 判断是否包含某属性
    /// </summary>
    public static bool HasProperty(this JsonElement? element, string propertyName)
        => element?.ValueKind == JsonValueKind.Object &&
           element.Value.TryGetProperty(propertyName, out _);

    #endregion

    #region 类型判断

    public static bool IsNull(this JsonElement? element)
        => element == null || element.Value.ValueKind == JsonValueKind.Null;

    public static bool IsNullOrUndefined(this JsonElement? element)
        => element == null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    public static bool IsObject(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Object;

    public static bool IsArray(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Array;

    public static bool IsString(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.String;

    public static bool IsNumber(this JsonElement? element)
        => element?.ValueKind == JsonValueKind.Number;

    public static bool IsBool(this JsonElement? element)
        => element?.ValueKind is JsonValueKind.True or JsonValueKind.False;

    public static bool Exists(this JsonElement? element)
        => element != null && element.Value.ValueKind != JsonValueKind.Undefined;

    #endregion

    #region 反序列化

    /// <summary>
    /// 反序列化为指定类型
    /// </summary>
    public static T? Deserialize<T>(this JsonElement? element, JsonSerializerOptions? options = null)
        => element.HasValue ? element.Value.Deserialize<T>(options) : default;

    /// <summary>
    /// 反序列化为指定类型，带默认值
    /// </summary>
    public static T Deserialize<T>(this JsonElement? element, T defaultValue, JsonSerializerOptions? options = null)
        => element.HasValue ? element.Value.Deserialize<T>(options) ?? defaultValue : defaultValue;

    #endregion

    #region 转换为字典/动态类型

    /// <summary>
    /// 转换为 Dictionary
    /// </summary>
    public static Dictionary<string, JsonElement>? ToDictionary(this JsonElement? element)
    {
        if (element?.ValueKind != JsonValueKind.Object)
            return null;

        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in element.Value.EnumerateObject())
            dict[prop.Name] = prop.Value;
        return dict;
    }

    #endregion

    #region 原始值

    /// <summary>
    /// 获取原始 JSON 字符串
    /// </summary>
    public static string? GetRawText(this JsonElement? element)
        => element?.GetRawText();

    #endregion
}
