using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Recharge;

public class RechargeGetListOutput
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal RechargeAmount { get; set; }
    public RechargeTypeEnum RechargeType { get; set; }
    public string Content { get; set; }
    public DateTime? ExpireDateTime { get; set; }
    public string Remark { get; set; }
    public DateTime CreationTime { get; set; }
}
