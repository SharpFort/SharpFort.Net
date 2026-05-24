using SqlSugar;
using SharpFort.Ai.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace SharpFort.Ai.Domain.Entities;

/// <summary>
/// AI知识库文档详情 - 记录导入的文档信息
/// </summary>
[SugarTable("Ai_KmsDetail")]
public class AiKmsDetail : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// 所属知识库ID
    /// </summary>
    public Guid? KmsId { get; set; }

    /// <summary>
    /// 关联文件ID
    /// </summary>
    public Guid? FileId { get; set; }

    /// <summary>
    /// 文档类型 (Text/Word/PDF/Markdown/Html)
    /// </summary>
    public string? FileType { get; set; }

    /// <summary>
    /// 文档内容
    /// </summary>
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Content { get; set; }

    /// <summary>
    /// 内容名称
    /// </summary>
    public string? ContentName { get; set; }

    /// <summary>
    /// 远程URL地址
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 数据处理量
    /// </summary>
    public int? DataCount { get; set; }

    /// <summary>
    /// 导入状态
    /// </summary>
    public ImportKmsStatus Status { get; set; } = ImportKmsStatus.Loading;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
