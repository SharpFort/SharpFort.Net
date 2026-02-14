using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Domain.Shared.Dtos;

public class AiModelDescribe
{
    /// <summary>
    /// 应用id
    /// </summary>
    public Guid AppId { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// 应用终结点
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// 应用key
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 处理名
    /// </summary>
    public string HandlerName { get; set; }

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
    public string? Description { get; set; }
    
    /// <summary>
    /// 额外url
    /// </summary>
    public string? AppExtraUrl { get; set; }
    
    /// <summary>
    /// 模型额外信息
    /// </summary>
    public string? ModelExtraInfo { get; set; }
    
    /// <summary>
    /// 模型倍率
    /// </summary>
    public decimal Multiplier { get; set; }
    
    /// <summary>
    /// 是否为尊享模型
    /// </summary>
    public bool IsPremium { get; set; }
    
    /// <summary>
    /// 模型类型(聊天/图片等)
    /// </summary>
    public ModelTypeEnum ModelType { get; set; }
}
