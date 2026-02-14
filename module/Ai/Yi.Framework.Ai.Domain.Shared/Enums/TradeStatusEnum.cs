using System.ComponentModel;

namespace Yi.Framework.Ai.Domain.Shared.Enums;

public enum TradeStatusEnum
{
    /// <summary>
    /// 准备发起
    /// </summary>
    [Description("准备发起")]
    WAIT_TRADE = 0,

    /// <summary>
    /// 交易创建
    /// </summary>
    [Description("等待买家付款")]
    WAIT_BUYER_PAY = 10,

    /// <summary>
    /// 交易关闭
    /// </summary>
    [Description("交易关闭")]
    TRADE_CLOSED = 20,

    /// <summary>
    /// 交易成功
    /// </summary>
    [Description("交易成功")]
    TRADE_SUCCESS = 100,

    /// <summary>
    /// 交易结束
    /// </summary>
    [Description("交易结束")]
    TRADE_FINISHED = -10
}
