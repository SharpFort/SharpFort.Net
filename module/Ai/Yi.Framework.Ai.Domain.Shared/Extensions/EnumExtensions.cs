using System.ComponentModel;
using System.Reflection;

namespace Yi.Framework.Ai.Domain.Shared.Extensions;

/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取枚举的Description特性值
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>Description特性值，如果没有则返回枚举名称</returns>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null)
        {
            return value.ToString();
        }

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}
