using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Model;

/// <summary>
/// 获取模型库列表查询参数
/// </summary>
public class ModelLibraryGetListInput : PagedAllResultRequestDto
{
    /// <summary>
    /// 搜索关键词（搜索模型名称、模型ID）
    /// </summary>
    public string? SearchKey { get; set; }

    /// <summary>
    /// 供应商名称筛选
    /// </summary>
    public List<string>? ProviderNames { get; set; }

    /// <summary>
    /// 模型类型筛选
    /// </summary>
    public List<ModelTypeEnum>? ModelTypes { get; set; }

    /// <summary>
    /// API类型筛选
    /// </summary>
    public List<ModelApiTypeEnum>? ModelApiTypes { get; set; }
}
