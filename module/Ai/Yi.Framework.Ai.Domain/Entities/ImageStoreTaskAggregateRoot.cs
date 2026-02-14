using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Domain.Entities;

[SugarTable("Ai_ImageStoreTask")]
public class ImageStoreTaskAggregateRoot : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 提示词
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string Prompt { get; set; }

    /// <summary>
    /// 参考图PrefixBase64（带前缀，如 data:image/png;base64,xxx）
    /// </summary>
    [SugarColumn(IsJson = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public List<string> ReferenceImagesPrefixBase64 { get; set; }

    /// <summary>
    /// 参考图url
    /// </summary>
    [SugarColumn(IsJson = true)]
    public List<string> ReferenceImagesUrl { get; set; }


    /// <summary>
    /// 图片绝对路径
    /// </summary>
    public string? StoreUrl { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatusEnum TaskStatus { get; set; } = TaskStatusEnum.Processing;

    /// <summary>
    /// 用户id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名称
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 模型id
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? ErrorInfo { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public PublishStatusEnum PublishStatus { get; set; } = PublishStatusEnum.Unpublished;

    /// <summary>
    /// 分类标签
    /// </summary>
    [SugarColumn(IsJson = true)]
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 是否匿名
    /// </summary>
    public bool IsAnonymous { get; set; } = false;
    
    /// <summary>
    /// 密钥id
    /// </summary>
    public Guid? TokenId { get; set; }

    /// <summary>
    /// 设置成功
    /// </summary>
    /// <param name="storeUrl"></param>
    public void SetSuccess(string storeUrl)
    {
        TaskStatus = TaskStatusEnum.Success;
        StoreUrl = storeUrl;
    }

    /// <summary>
    /// 设置发布
    /// </summary>
    /// <param name="isAnonymous"></param>
    /// <param name="categories"></param>
    public void SetPublish(bool isAnonymous,List<string> categories)
    {
        this.PublishStatus = PublishStatusEnum.Published;
        this.IsAnonymous = isAnonymous;
        this.Categories = categories;
    }


}