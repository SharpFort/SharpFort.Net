using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 消息通知类型/范围
/// </summary>
public enum NoticeType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 个人通知
    /// </summary>
    [Description("个人")]
    Personal = 10,

    /// <summary>
    /// 全站广播
    /// </summary>
    [Description("广播")]
    Broadcast = 20,

    /// <summary>
    /// 资金/积分变动通知
    /// </summary>
    [Description("资金")]
    Money = 30
}