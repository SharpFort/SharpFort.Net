using System.ComponentModel;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Contracts.Dtos.AiKms;

/// <summary>
/// AI知识库DTO
/// </summary>
public class AiKmsDto : EntityDto<Guid>
{
    /// <summary>
    /// 知识库名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 每段落最大Token数
    /// </summary>
    [DefaultValue(299)]
    public int MaxTokensPerParagraph { get; set; } = 299;

    /// <summary>
    /// 每行最大Token数
    /// </summary>
    [DefaultValue(99)]
    public int MaxTokensPerLine { get; set; } = 99;

    /// <summary>
    /// 段落间重叠Token数
    /// </summary>
    [DefaultValue(49)]
    public int OverlappingTokens { get; set; } = 49;

    /// <summary>
    /// 矢量化模型ID
    /// </summary>
    public Guid? AiModelId { get; set; }

    /// <summary>
    /// 文档数量
    /// </summary>
    public int DocumentCount => AiKmsDetailList?.Count ?? 0;

    /// <summary>
    /// 状态: 0=待处理 1=处理中 2=已完成 3=失败
    /// </summary>
    public int Status => GetStatus();

    /// <summary>
    /// 文档详情列表
    /// </summary>
    public List<AiKmsDetailDto> AiKmsDetailList { get; set; } = [];

    private int GetStatus()
    {
        if (AiKmsDetailList.Any(t => t.Status == Domain.Shared.Enums.ImportKmsStatus.Fail))
            return 3;
        if (AiKmsDetailList.Any(t => t.Status == Domain.Shared.Enums.ImportKmsStatus.Loading))
            return 1;
        return 2;
    }
}
