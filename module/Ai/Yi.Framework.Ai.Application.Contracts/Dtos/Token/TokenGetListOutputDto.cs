namespace Yi.Framework.Ai.Application.Contracts.Dtos.Token;

/// <summary>
/// Token列表输出
/// </summary>
public class TokenGetListOutputDto
{
    /// <summary>
    /// Token Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Token密钥
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 过期时间（空为永不过期）
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否启用请求日志记录
    /// </summary>
    public bool IsEnableLog { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}
