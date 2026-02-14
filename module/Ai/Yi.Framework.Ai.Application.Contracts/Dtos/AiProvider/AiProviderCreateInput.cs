using System.ComponentModel.DataAnnotations;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.AiProvider;

/// <summary>
/// 创建AI供应商输入
/// </summary>
public class AiProviderCreateInput
{
    /// <summary>
    /// 供应商名称
    /// </summary>
    [Required(ErrorMessage = "供应商名称不能为空")]
    [StringLength(100, ErrorMessage = "供应商名称不能超过100个字符")]
    public string Name { get; set; }

    /// <summary>
    /// API终结点
    /// </summary>
    [Required(ErrorMessage = "API终结点不能为空")]
    [StringLength(500, ErrorMessage = "API终结点不能超过500个字符")]
    public string Endpoint { get; set; }

    /// <summary>
    /// 额外URL
    /// </summary>
    [StringLength(500, ErrorMessage = "额外URL不能超过500个字符")]
    public string? ExtraUrl { get; set; }

    /// <summary>
    /// API Key
    /// </summary>
    [Required(ErrorMessage = "API Key不能为空")]
    [StringLength(500, ErrorMessage = "API Key不能超过500个字符")]
    public string ApiKey { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "排序必须大于等于0")]
    public int OrderNum { get; set; }
}
