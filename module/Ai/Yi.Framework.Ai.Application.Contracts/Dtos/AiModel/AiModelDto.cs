using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiModel;

/// <summary>
/// AI模型DTO
/// </summary>
public class AiModelDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// 处理程序名称
    /// </summary>
    public string HandlerName { get; set; }

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 供应商ID
    /// </summary>
    public Guid AiProviderId { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    public string? ExtraInfo { get; set; }

    /// <summary>
    /// 模型类型
    /// </summary>
    public ModelTypeEnum ModelType { get; set; }

    /// <summary>
    /// 模型API类型
    /// </summary>
    public ModelApiTypeEnum ModelApiType { get; set; }

    /// <summary>
    /// 成本倍率
    /// </summary>
    public decimal Multiplier { get; set; }

    /// <summary>
    /// 显示倍率
    /// </summary>
    public decimal MultiplierShow { get; set; }

    /// <summary>
    /// 供应商分组名称
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// 图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
