using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 用户账号状态
/// </summary>
public enum UserSafetyStatus
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 正常状态
    /// </summary>
    [Description("正常")]
    Normal = 10, // 原 0 (Normal)，需数据迁移

    /// <summary>
    /// 高风险/受限 (原 Dangerous)
    /// </summary>
    [Description("高风险")]
    HighRisk = 20,

    /// <summary>
    /// 已封禁 (原 Ban)
    /// </summary>
    [Description("已封禁")]
    Banned = 30
}
