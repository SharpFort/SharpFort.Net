using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Rbac.Domain.Shared.Enums;
/// <summary>
/// 公告展示类型
/// </summary>
public enum NoticeType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 走马灯
    /// </summary>
    [Description("走马灯")]
    MerryGoRound = 10,

    /// <summary>
    /// 提示弹窗
    /// </summary>
    [Description("提示弹窗")]
    Popup = 20
}

