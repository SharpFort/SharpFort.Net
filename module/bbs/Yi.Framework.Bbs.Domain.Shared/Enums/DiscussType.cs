using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 主题帖类型
/// </summary>
public enum DiscussType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 普通文章
    /// </summary>
    [Description("文章")]
    Article = 10, // 原 0，需数据迁移

    /// <summary>
    /// 悬赏帖
    /// </summary>
    [Description("悬赏")]
    Reward = 20 // 原 1
}