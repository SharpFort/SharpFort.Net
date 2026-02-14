using Volo.Abp.Application.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiUsage;

public class UsageStatisticsGetInput : PagedResultRequestDto
{
    public Guid? TokenId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
