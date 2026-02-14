using System.ComponentModel.DataAnnotations;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Token;

/// <summary>
/// 编辑Token输入
/// </summary>
public class TokenUpdateInput
{
    /// <summary>
    /// Token Id
    /// </summary>
    [Required(ErrorMessage = "Id不能为空")]
    public Guid Id { get; set; }

    /// <summary>
    /// 名称（同一用户不能重复）
    /// </summary>
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    public string Name { get; set; }

    /// <summary>
    /// 过期时间（空为永不过期）
    /// </summary>
    public DateTime? ExpireTime { get; set; }


}
