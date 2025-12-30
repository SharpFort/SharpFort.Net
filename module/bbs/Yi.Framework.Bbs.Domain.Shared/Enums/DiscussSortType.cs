using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;
/// <summary>
/// 主题列表排序/筛选类型
/// </summary>
public enum DiscussSortType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0, 

    /// <summary>
    /// 最新发布 (New)
    /// </summary>
    [Description("最新")]
    Latest = 10, // 原 New

    /// <summary>
    /// 推荐/精华 (Suggest)
    /// </summary>
    [Description("推荐")]
    Recommended = 20, // 原 Suggest

    /// <summary>
    /// 热门讨论 (Hot)
    /// </summary>
    [Description("热门")]
    Hot = 30 // 原 Host (修正拼写)
}