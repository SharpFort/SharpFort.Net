using Volo.Abp.Application.Dtos;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.UsageStatistics;

public class PremiumTokenUsageGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 是否免费
    /// </summary>
    public bool? IsFree { get; set; }
}
