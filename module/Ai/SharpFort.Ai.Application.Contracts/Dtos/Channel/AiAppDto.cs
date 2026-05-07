namespace SharpFort.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// AI应用DTO
/// </summary>
public class AiAppDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 应用终结点
    /// </summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// 额外URL
    /// </summary>
    public string? ExtraUrl { get; set; }

    /// <summary>
    /// 应用Key
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}
