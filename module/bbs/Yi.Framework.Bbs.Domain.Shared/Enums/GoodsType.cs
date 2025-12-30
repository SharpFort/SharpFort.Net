using System.ComponentModel;

namespace Yi.Framework.Bbs.Domain.Shared.Enums;


/// <summary>
/// 积分商城商品类型
/// </summary>
public enum GoodsType
{
    /// <summary>
    /// 未知/默认
    /// </summary>
    [Description("未知")]
    None = 0,

    /// <summary>
    /// 申请类商品 (如内测资格、实物兑换申请)
    /// </summary>
    [Description("申请")]
    Apply = 10
}