using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// AI应用快捷配置表
/// </summary>
[SugarTable("Ai_App_Shortcut")]
public class AiAppShortcutAggregateRoot : FullAuditedAggregateRoot<Guid>, IOrderNum
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 应用终结点
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// 额外url
    /// </summary>
    public string? ExtraUrl { get; set; }

    /// <summary>
    /// 应用key
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }
}
