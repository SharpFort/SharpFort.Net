using System.ComponentModel;

namespace Yi.Framework.Rbac.Domain.Shared.Enums;

public enum MenuSource
{
    /// <summary>
    /// RuoYi
    /// </summary>
    [Description("RuoYi")]
    Ruoyi = 0,

    /// <summary>
    /// PureAdmin
    /// </summary>
    [Description("PureAdmin")]
    Pure = 1
}