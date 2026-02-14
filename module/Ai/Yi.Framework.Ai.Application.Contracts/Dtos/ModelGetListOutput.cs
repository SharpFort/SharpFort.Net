using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class ModelGetListOutput
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 模型id
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; }

    /// <summary>
    /// 模型描述
    /// </summary>
    public string? ModelDescribe { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 是否为尊享包
    /// </summary>
    public bool IsPremiumPackage { get; set; }

    /// <summary>
    /// 是否免费模型
    /// </summary>
    public bool IsFree { get; set; }
    
    /// <summary>
    /// 模型Api类型，现支持同一个模型id，多种接口格式
    /// </summary>
    public ModelApiTypeEnum ModelApiType { get; set; }
    
    /// <summary>
    /// 模型图标URL
    /// </summary>
    public string? IconUrl { get; set; }
    /// <summary>
    /// 供应商分组名称(如：OpenAI、Anthropic、Google等)
    /// </summary>
    public string? ProviderName { get; set; }
}
