using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 图片任务输出
/// </summary>
public class ImageTaskOutput
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 提示词
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// 是否匿名
    /// </summary>
    public bool IsAnonymous { get; set; } 

    /// <summary>
    /// 生成图片URL
    /// </summary>
    public string? StoreUrl { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatusEnum TaskStatus { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public PublishStatusEnum PublishStatus { get; set; }

    /// <summary>
    /// 分类标签
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorInfo { get; set; }
    
    /// <summary>
    /// 用户名称
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// 用户名称Id
    /// </summary>
    public Guid? UserId { get; set; }
    
}
