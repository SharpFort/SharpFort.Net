using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// ai黑名单
/// </summary>
[SugarTable("Ai_Blacklist")]
public class AiBlacklist : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 用户
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// 有效开始时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 有效结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
}
