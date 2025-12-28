using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 文章导入数据源类型
/// </summary>
public enum ArticleImportType
{
    /// <summary>
    /// 未知/无
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 默认/通用导入
    /// </summary>
    [Description("默认导入")]
    General = 10, // 原 Default，建议改为 General 或 Markdown 以示明确

    /// <summary>
    /// VuePress 格式
    /// </summary>
    [Description("VuePress")]
    VuePress = 20
}
