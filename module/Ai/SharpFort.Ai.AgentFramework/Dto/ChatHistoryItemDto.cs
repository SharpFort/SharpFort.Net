using Microsoft.Extensions.VectorData;

namespace SharpFort.Ai.AgentFramework.Dto;

/// <summary>
/// AI消息DTO
/// </summary>
public class ChatHistoryItemDto
{
    [VectorStoreKey]
    public string? Key { get; set; }

    [VectorStoreData]
    public string? ThreadId { get; set; }

    [VectorStoreData]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// 整个消息JSON
    /// </summary>
    [VectorStoreData]
    public string? SerializedMessage { get; set; }

    [VectorStoreData]
    public string? MessageText { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [VectorStoreData]
    public string? Role { get; set; }

    public string? MessageId { get; set; }
}
