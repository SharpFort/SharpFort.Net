using Volo.Abp.Application.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Recharge;

public class RechargeGetListInput : PagedAndSortedResultRequestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool? IsFree { get; set; }
    public decimal? MinRechargeAmount { get; set; }
    public decimal? MaxRechargeAmount { get; set; }
}
