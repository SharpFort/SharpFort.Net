using Volo.Abp.Application.Dtos;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiProvider;

/// <summary>
/// AI供应商/应用配置DTO
/// </summary>
public class AiProviderDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// 供应商名称
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
}
