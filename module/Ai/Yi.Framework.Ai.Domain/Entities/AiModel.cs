using SqlSugar;
using Volo.Abp.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Core.Data;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// AI模型定义
/// </summary>
[SugarTable("Ai_Model")]
public class AiModel : Entity<Guid>, IOrderNum, ISoftDelete
{
    public AiModel()
    {
    }

    /// <summary>
    /// 处理程序名称 (e.g. OpenAIHandler)
    /// </summary>
    public string HandlerName { get; set; }

    /// <summary>
    /// 模型ID (e.g. gpt-4)
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 显示名称
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
    /// 软删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 关联的供应商ID
    /// </summary>
    public Guid AiProviderId { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    public string? ExtraInfo { get; set; }

    /// <summary>
    /// 模型类型(聊天/图片等)
    /// </summary>
    public ModelTypeEnum ModelType { get; set; }

    /// <summary>
    /// 模型Api类型
    /// </summary>
    public ModelApiTypeEnum ModelApiType { get; set; }

    /// <summary>
    /// 成本倍率
    /// </summary>
    public decimal Multiplier { get; set; } = 1;
    
    /// <summary>
    /// 显示倍率
    /// </summary>
    public decimal MultiplierShow { get; set; } = 1;

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
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否为尊享模型
    /// </summary>
    public bool IsPremium { get; set; } = false;
}
