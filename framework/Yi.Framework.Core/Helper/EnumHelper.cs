using System;
using System.ComponentModel;
using System.Reflection;

namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// 枚举辅助类
    /// 提供枚举类型转换、字符串解析、描述获取等功能
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 将一个枚举类型转换为另一个枚举类型（基于枚举值）
        /// </summary>
        /// <typeparam name="New">目标枚举类型</typeparam>
        /// <param name="oldEnum">源枚举值</param>
        /// <returns>转换后的枚举值</returns>
        /// <exception cref="ArgumentNullException">源枚举为 null</exception>
        /// <example>
        /// enum OldStatus { Active = 1, Inactive = 2 }
        /// enum NewStatus { Active = 1, Inactive = 2 }
        /// OldStatus.Active.EnumToEnum&lt;NewStatus&gt;() => NewStatus.Active
        /// </example>
        public static New EnumToEnum<New>(this object oldEnum)
        {
            if (oldEnum is null)
            {
                throw new ArgumentNullException(nameof(oldEnum));
            }
            return (New)Enum.ToObject(typeof(New), oldEnum.GetHashCode());
        }

        /// <summary>
        /// 将字符串解析为枚举值（不区分大小写）
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="str">枚举名称字符串</param>
        /// <returns>枚举值</returns>
        /// <exception cref="ArgumentException">字符串无法解析为枚举</exception>
        /// <example>
        /// "Active".StringToEnum&lt;Status&gt;() => Status.Active
        /// </example>
        public static TEnum StringToEnum<TEnum>(this string str)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }

        #region 新增方法 - 数据库字符串存储支持

        /// <summary>
        /// 将枚举值转换为字符串名称（用于数据库字符串存储）
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>枚举名称字符串</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 数据库存储枚举名称而非数值，提高可读性
        /// 2. 配置文件中使用枚举名称
        /// 3. API 返回枚举字符串表示
        /// </remarks>
        /// <example>
        /// Gender.Male.ToEnumString() => "Male"
        /// Status.Active.ToEnumString() => "Active"
        /// </example>
        public static string ToEnumString<TEnum>(this TEnum enumValue) where TEnum : Enum
        {
            return enumValue.ToString();
        }

        /// <summary>
        /// 从字符串名称解析枚举值（用于数据库读取）
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="str">枚举名称字符串</param>
        /// <param name="ignoreCase">是否忽略大小写，默认 true</param>
        /// <returns>枚举值</returns>
        /// <exception cref="ArgumentException">字符串无法解析为枚举</exception>
        /// <remarks>
        /// 使用场景：
        /// 1. 从数据库读取枚举字符串并转换为枚举类型
        /// 2. 从配置文件解析枚举值
        /// 3. API 参数字符串转枚举
        /// </remarks>
        /// <example>
        /// "Male".FromEnumString&lt;Gender&gt;() => Gender.Male
        /// "male".FromEnumString&lt;Gender&gt;() => Gender.Male (忽略大小写)
        /// "ACTIVE".FromEnumString&lt;Status&gt;(ignoreCase: false) => 抛出异常
        /// </example>
        public static TEnum FromEnumString<TEnum>(this string str, bool ignoreCase = true)
            where TEnum : struct, Enum
        {
            return Enum.Parse<TEnum>(str, ignoreCase);
        }

        /// <summary>
        /// 安全地从字符串解析枚举值（失败返回默认值）
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="str">枚举名称字符串</param>
        /// <param name="defaultValue">解析失败时的默认值</param>
        /// <param name="ignoreCase">是否忽略大小写，默认 true</param>
        /// <returns>枚举值或默认值</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 用户输入验证，提供容错机制
        /// 2. 外部数据导入，避免因非法值导致程序崩溃
        /// 3. 配置文件读取，提供默认值
        /// </remarks>
        /// <example>
        /// "Unknown".TryParseEnumString(Gender.Unknown) => Gender.Unknown
        /// "InvalidValue".TryParseEnumString(Status.Inactive) => Status.Inactive (返回默认值)
        /// "active".TryParseEnumString(Status.Inactive) => Status.Active (成功解析)
        /// </example>
        public static TEnum TryParseEnumString<TEnum>(this string str, TEnum defaultValue, bool ignoreCase = true)
            where TEnum : struct, Enum
        {
            return Enum.TryParse<TEnum>(str, ignoreCase, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取枚举值的 Description 特性描述（用于前端显示）
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>Description 特性值，无特性则返回枚举名称</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 前端下拉列表显示枚举的中文描述
        /// 2. 日志记录时显示可读的枚举描述
        /// 3. 报表生成时展示枚举的业务含义
        ///
        /// 使用 Description 特性标记枚举：
        /// <code>
        /// public enum Gender
        /// {
        ///     [Description("未知")]
        ///     Unknown = 0,
        ///     [Description("男性")]
        ///     Male = 1,
        ///     [Description("女性")]
        ///     Female = 2
        /// }
        /// </code>
        /// </remarks>
        /// <example>
        /// // 有 Description 特性的枚举
        /// [Description("男性")] Male => "男性"
        ///
        /// // 无 Description 特性的枚举
        /// Female => "Female"
        /// </example>
        public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : Enum
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttribute?.Description ?? enumValue.ToString();
        }

        #endregion
    }
}
