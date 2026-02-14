using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// AI供应商/应用配置
/// </summary>
[SugarTable("Ai_Provider")]
public class AiProvider : FullAuditedAggregateRoot<Guid>, IOrderNum
{
    public AiProvider()
    {
    }

    /// <summary>
    /// 供应商名称 (e.g. OpenAI, DeepSeek)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// API终结点
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// 额外URL
    /// </summary>
    public string? ExtraUrl { get; set; }

    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 关联的模型
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(AiModel.AiProviderId))]
    public List<AiModel> AiModels { get; set; }
}
