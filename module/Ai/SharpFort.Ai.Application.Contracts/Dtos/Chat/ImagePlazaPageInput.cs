using Volo.Abp.Application.Dtos;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 图片任务分页查询输入
/// </summary>
public class ImagePlazaPageInput: PagedAllResultRequestDto
{
    /// <summary>
    /// 分类
    /// </summary>
    public string? Categories { get; set; }
    
    /// <summary>
    /// 提示词
    /// </summary>
    public string? Prompt { get; set; }
    
    /// <summary>
    /// 任务状态筛选（可选）
    /// </summary>
    public TaskStatusEnum? TaskStatusEnum { get; set; }
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName{ get; set; }
}
