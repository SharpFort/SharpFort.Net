using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// AI充值记录/VIP记录
/// </summary>
public class AiRecharge : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public RechargeTypeEnum RechargeType { get; set; }
    public decimal RechargeAmount { get; set; }
    
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Content { get; set; }
    
    public string? Remark { get; set; }
    public string? ContactInfo { get; set; }
    
    public DateTime? ExpireDateTime { get; set; }
}
