using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 图片任务分页查询输入
/// </summary>
public class ImageMyTaskPageInput: PagedAllResultRequestDto
{
    /// <summary>
    /// 提示词
    /// </summary>
    public string? Prompt { get; set; }
    
    /// <summary>
    /// 任务状态筛选（可选）
    /// </summary>
    public TaskStatusEnum? TaskStatus { get; set; }
    
    /// <summary>
    /// 发布状态
    /// </summary>
    public PublishStatusEnum? PublishStatus { get; set; }
}
