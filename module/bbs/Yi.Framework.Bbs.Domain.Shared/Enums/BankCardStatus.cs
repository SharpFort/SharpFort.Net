using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 银行卡/资源卡槽状态
/// </summary>
public enum BankCardStatus
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 闲置/未使用
    /// </summary>
    [Description("闲置")]
    Idle = 10, // 原 Unused。0值必须留给None，业务值后移

    /// <summary>
    /// 等待中
    /// </summary>
    [Description("等待中")]
    Waiting = 20, // 原 Wait

    /// <summary>
    /// 存储已满
    /// </summary>
    [Description("已满")]
    Full = 30
}