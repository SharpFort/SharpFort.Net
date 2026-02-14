using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Ai.Domain.Shared.Extensions;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Model;

/// <summary>
/// 模型库展示数据
/// </summary>
public class ModelLibraryDto
{
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
    /// 模型类型
    /// </summary>
    public ModelTypeEnum ModelType { get; set; }

    /// <summary>
    /// 模型类型名称
    /// </summary>
    public string ModelTypeName => ModelType.GetDescription();

    /// <summary>
    /// 模型支持的API类型
    /// </summary>
    public List<ModelApiTypeOutput> ModelApiTypes { get; set; }

    
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
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }
}

public class ModelApiTypeOutput
{
    /// <summary>
    /// 模型类型
    /// </summary>
    public ModelApiTypeEnum ModelApiType { get; set; }

    /// <summary>
    /// 模型类型名称
    /// </summary>
    public string ModelApiTypeName => ModelApiType.GetDescription();
}
