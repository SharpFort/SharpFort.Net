using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;
/// <summary>
/// 主题帖访问权限类型
/// </summary>
public enum DiscussPermissionType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 公开 (所有人可见)
    /// </summary>
    [Description("公开")]
    Public = 10, // 原 0，建议迁移

    /// <summary>
    /// 仅指定角色可见
    /// </summary>
    [Description("角色可见")]
    Role = 20 // 原 1
}