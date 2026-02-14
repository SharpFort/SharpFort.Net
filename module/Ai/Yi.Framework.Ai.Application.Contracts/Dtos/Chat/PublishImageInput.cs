namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 发布图片输入
/// </summary>
public class PublishImageInput
{
    /// <summary>
    /// 是否匿名
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// 任务ID
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 分类标签
    /// </summary>
    public List<string> Categories { get; set; } = new();
}
