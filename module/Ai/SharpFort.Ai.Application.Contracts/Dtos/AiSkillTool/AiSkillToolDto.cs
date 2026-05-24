using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SharpFort.Ai.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Contracts.Dtos.AiSkillTool;

/// <summary>
/// AI技能/工具管理DTO
/// </summary>
public class AiSkillToolDto : EntityDto<Guid>
{
    /// <summary>
    /// 技能/工具名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// 类方法名 (Tool类型使用)
    /// </summary>
    [StringLength(200)]
    public string? ClassMethod { get; set; }

    /// <summary>
    /// 技能/工具描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否系统内置
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
