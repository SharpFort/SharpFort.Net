using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI知识库配置 - 定义文档分块策略和向量化模型
/// </summary>
[SugarTable("Ai_Kms")]
public class AiKms : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 知识库名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 每段落最大Token数
    /// </summary>
    public int MaxTokensPerParagraph { get; set; } = 299;

    /// <summary>
    /// 每行最大Token数
    /// </summary>
    public int MaxTokensPerLine { get; set; } = 99;

    /// <summary>
    /// 段落间重叠Token数
    /// </summary>
    public int OverlappingTokens { get; set; } = 49;

    /// <summary>
    /// 矢量化模型ID
    /// </summary>
    public Guid? AiModelId { get; set; }
}
