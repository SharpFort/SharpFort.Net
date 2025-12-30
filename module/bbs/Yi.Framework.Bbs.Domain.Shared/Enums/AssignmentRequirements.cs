using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 任务完成条件要求 (支持多选)
/// </summary>
[Flags]
public enum AssignmentRequirements
{
    /// <summary>
    /// 无要求
    /// </summary>
    [Description("无")]
    None = 0,

    /// <summary>
    /// 发布主题
    /// </summary>
    [Description("发布主题")]
    Discuss = 1, // 2^0

    /// <summary>
    /// 发布评论
    /// </summary>
    [Description("发布评论")]
    Comment = 2, // 2^1

    /// <summary>
    /// 点赞互动
    /// </summary>
    [Description("点赞")]
    Agree = 4, // 2^2

    /// <summary>
    /// 更新昵称
    /// </summary>
    [Description("更新昵称")]
    UpdateNick = 8, // 2^3

    /// <summary>
    /// 更新头像
    /// </summary>
    [Description("更新头像")]
    UpdateIcon = 16 // 2^4
}