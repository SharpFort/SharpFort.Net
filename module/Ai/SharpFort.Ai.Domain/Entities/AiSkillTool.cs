using SqlSugar;
using SharpFort.Ai.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI技能/工具注册管理
/// </summary>
[SugarTable("Ai_SkillTool")]
public class AiSkillTool : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 技能/工具名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? Name { get; set; }

    /// <summary>
    /// 类方法名 (Tool类型使用)
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? ClassMethod { get; set; }

    /// <summary>
    /// 技能/工具描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否系统内置（内置工具不允许删除和修改）
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 技能/工具类型
    /// </summary>
    public AiSkillToolType SkillToolType { get; set; } = AiSkillToolType.Tool;
}
