using System.ComponentModel.DataAnnotations;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

/// <summary>
/// 更新AI模型输入
/// </summary>
public class AiModelUpdateInput
{
    /// <summary>
    /// 模型ID
    /// </summary>
    [Required(ErrorMessage = "模型ID不能为空")]
    public Guid Id { get; set; }

    /// <summary>
    /// 处理名
    /// </summary>
    [Required(ErrorMessage = "处理名不能为空")]
    [StringLength(100, ErrorMessage = "处理名不能超过100个字符")]
    public string HandlerName { get; set; }

    /// <summary>
    /// 模型ID
    /// </summary>
    [Required(ErrorMessage = "模型ID不能为空")]
    [StringLength(200, ErrorMessage = "模型ID不能超过200个字符")]
    public string ModelId { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    [Required(ErrorMessage = "模型名称不能为空")]
    [StringLength(200, ErrorMessage = "模型名称不能超过200个字符")]
    public string Name { get; set; }

    /// <summary>
    /// 模型描述
    /// </summary>
    [StringLength(1000, ErrorMessage = "模型描述不能超过1000个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "排序必须大于等于0")]
    public int OrderNum { get; set; }

    /// <summary>
    /// AI应用ID
    /// </summary>
    [Required(ErrorMessage = "AI应用ID不能为空")]
    public Guid AiAppId { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    [StringLength(2000, ErrorMessage = "额外信息不能超过2000个字符")]
    public string? ExtraInfo { get; set; }

    /// <summary>
    /// 模型类型
    /// </summary>
    [Required(ErrorMessage = "模型类型不能为空")]
    public ModelTypeEnum ModelType { get; set; }

    /// <summary>
    /// 模型API类型
    /// </summary>
    [Required(ErrorMessage = "模型API类型不能为空")]
    public ModelApiTypeEnum ModelApiType { get; set; }

    /// <summary>
    /// 模型倍率
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "模型倍率必须大于0")]
    public decimal Multiplier { get; set; }

    /// <summary>
    /// 模型显示倍率
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "模型显示倍率必须大于0")]
    public decimal MultiplierShow { get; set; }

    /// <summary>
    /// 供应商分组名称
    /// </summary>
    [StringLength(100, ErrorMessage = "供应商分组名称不能超过100个字符")]
    public string? ProviderName { get; set; }

    /// <summary>
    /// 模型图标URL
    /// </summary>
    [StringLength(500, ErrorMessage = "模型图标URL不能超过500个字符")]
    public string? IconUrl { get; set; }


    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
