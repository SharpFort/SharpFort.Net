namespace Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// AI应用快捷配置DTO
/// </summary>
public class AiAppShortcutDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 应用终结点
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// 额外URL
    /// </summary>
    public string? ExtraUrl { get; set; }

    /// <summary>
    /// 应用Key
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}
