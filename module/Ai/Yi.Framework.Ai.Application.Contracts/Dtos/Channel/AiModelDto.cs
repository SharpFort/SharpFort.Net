using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// AI模型DTO
/// </summary>
public class AiModelDto
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 处理名
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
    /// 模型描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// AI应用ID
    /// </summary>
    public Guid AiAppId { get; set; }

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
    /// 模型倍率
    /// </summary>
    public decimal Multiplier { get; set; }

    /// <summary>
    /// 模型显示倍率
    /// </summary>
    public decimal MultiplierShow { get; set; }

    /// <summary>
    /// 供应商分组名称
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// 模型图标URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
