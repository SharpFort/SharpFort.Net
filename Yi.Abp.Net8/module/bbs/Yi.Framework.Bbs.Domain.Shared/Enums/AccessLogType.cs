using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;

/// <summary>
/// 访问日志类型
/// </summary>
public enum AccessLogType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 首页访问
    /// </summary>
    [Description("首页访问")]
    HomeVisit = 10, // 原 HomeVisit，Visit 语义更通用

    /// <summary>
    /// 接口请求
    /// </summary>
    [Description("接口请求")]
    ApiRequest = 20 // 原 Request，加 Api 前缀更明确
}