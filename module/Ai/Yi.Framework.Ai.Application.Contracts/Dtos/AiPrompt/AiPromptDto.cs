using Volo.Abp.Application.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiPrompt;

/// <summary>
/// AI提示词模板DTO
/// </summary>
public class AiPromptDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// 提示词编码
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 提示词内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 默认关联的模型ID
    /// </summary>
    public Guid? DefaultModelId { get; set; }
}
