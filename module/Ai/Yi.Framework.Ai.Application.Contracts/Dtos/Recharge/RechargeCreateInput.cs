using System.ComponentModel.DataAnnotations;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Recharge;

public class RechargeCreateInput
{
    public Guid UserId { get; set; }
    public decimal RechargeAmount { get; set; }
    public string Content { get; set; }
    public int? Months { get; set; }
    public int? Days { get; set; } // Added based on usage in RechargeService
    public string Remark { get; set; }
    public string ContactInfo { get; set; }
}
