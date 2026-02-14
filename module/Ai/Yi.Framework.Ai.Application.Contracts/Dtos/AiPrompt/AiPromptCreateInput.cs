using System.ComponentModel.DataAnnotations;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiPrompt;

public class AiPromptCreateInput
{
    /// <summary>
    /// 提示词编码
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Code { get; set; }

    /// <summary>
    /// 提示词内容
    /// </summary>
    [Required]
    public string Content { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 默认关联的模型ID
    /// </summary>
    public Guid? DefaultModelId { get; set; }
}
