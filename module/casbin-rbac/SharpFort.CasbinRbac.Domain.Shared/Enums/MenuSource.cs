using System.ComponentModel;

namespace SharpFort.CasbinRbac.Domain.Shared.Enums;

public enum MenuSource
{
    /// <summary>
    /// RuoSf
    /// </summary>
    [Description("RuoSf")]
    Ruoyi = 0,

    /// <summary>
    /// PureAdmin
    /// </summary>
    [Description("PureAdmin")]
    Pure = 1
}