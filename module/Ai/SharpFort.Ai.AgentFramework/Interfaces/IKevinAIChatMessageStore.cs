using SharpFort.Ai.AgentFramework.Dto;

namespace SharpFort.Ai.AgentFramework.Interfaces;

/// <summary>
/// AI消息存储接口
/// </summary>
public interface IKevinAIChatMessageStore
{
    Task AddMessagesAsync(List<ChatHistoryItemDto> chatHistoryItems, CancellationToken cancellationToken);
    Task<List<ChatHistoryItemDto>> GetMessagesAsync(string threadId, CancellationToken cancellationToken);
}
