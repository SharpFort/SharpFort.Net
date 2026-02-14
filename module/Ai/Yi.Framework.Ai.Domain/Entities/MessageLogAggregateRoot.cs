using SqlSugar;
using Volo.Abp.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Domain.Entities;

[SugarTable("Ai_Message_Log")]
public class MessageLogAggregateRoot : Entity<Guid>
{
    /// <summary>
    /// 请求内容（httpbody）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? RequestBody { get; set; }

    /// <summary>
    /// 请求apikey
    /// </summary>
    [SugarColumn(Length = 255)]
    public string ApiKey { get; set; }

    /// <summary>
    /// 请求apikey名称
    /// </summary>
    [SugarColumn(Length = 255)]
    public string ApiKeyName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 模型id
    /// </summary>
    [SugarColumn(Length = 64)]
    public string ModelId { get; set; }

    /// <summary>
    /// api类型
    /// </summary>
    public ModelApiTypeEnum ApiType { get; set; }

    /// <summary>
    /// api类型名称
    /// </summary>
    [SugarColumn(Length = 16)]
    public string ApiTypeName { get; set; }
}
