using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// AI提示词模板
/// </summary>
[SugarTable("Ai_Prompt")]
public class AiPrompt : FullAuditedAggregateRoot<Guid>
{
    public AiPrompt()
    {
    }

    /// <summary>
    /// 提示词编码 (唯一标识)
    /// </summary>
    [SugarColumn(IsNullable = false, UniqueGroupNameList = new []{"uk_code"})]
    public string Code { get; set; }

    /// <summary>
    /// 提示词内容
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string Content { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 默认关联的模型ID (可选)
    /// </summary>
    public Guid? DefaultModelId { get; set; }
}
