using System.ComponentModel;

namespace Yi.Framework.CasbinRbac.Domain.Shared.Enums;

/// <summary>
/// 手机号验证场景类型
/// </summary>
public enum  PhoneValidationType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 用户注册
    /// </summary>
    [Description("注册")]
    Register = 10,

    /// <summary>
    /// 找回密码
    /// </summary>
    [Description("忘记密码")]
    RetrievePassword = 20,

    /// <summary>
    /// 绑定手机号
    /// </summary>
    [Description("绑定")]
    Bind = 30
}