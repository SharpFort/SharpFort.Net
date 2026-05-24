using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI技能/工具绑定关系 - 将Skill/Tool绑定到应用(Agent)或用户(User)
/// </summary>
[SugarTable("Ai_SkillToolBind")]
public class AiSkillToolBind : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 技能/工具管理ID
    /// </summary>
    public Guid AiSkillToolId { get; set; }

    /// <summary>
    /// 关联ID (AiApp Id / User Id / Role Id)
    /// </summary>
    [SugarColumn(Length = 100)]
    public string? BindId { get; set; }
}
