using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 任务分配状态
/// </summary>
public enum AssignmentStatus
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 正在进行中
    /// </summary>
    [Description("进行中")]
    InProgress = 10, // 原 Progress

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed = 20,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 30,

    /// <summary>
    /// 已结束/已关闭 (通常指手动结束或非正常完成)
    /// </summary>
    [Description("已结束")]
    Closed = 40 // 原 End，建议确认业务含义是否为“取消”或“关闭”
}