namespace SharpFort.Ai.Application.Contracts.Dtos.AiApp;

/// <summary>
/// AI应用绑定技能/工具DTO
/// </summary>
public class AiAppBindSkillToolDto
{
    /// <summary>
    /// 技能/工具管理ID
    /// </summary>
    public Guid AiSkillToolId { get; set; }

    /// <summary>
    /// 技能/工具名称
    /// </summary>
    public string? AiSkillToolName { get; set; }

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool IsSelect { get; set; }
}
