using System.ComponentModel;

namespace Yi.Framework.Ai.Domain.Shared.Enums;

public enum RechargeTypeEnum
{
    [Description("VIP Recharge")]
    Vip = 1,
    
    [Description("Token Recharge")]
    Token = 2
}
