using SqlSugar;
using Volo.Abp.Application.Services;
using SharpFort.Ai.AgentFramework.Dto;
using SharpFort.Ai.AgentFramework.Interfaces;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class AiChatMessageStoreService(
    ISqlSugarRepository<AiChatMessageStore, Guid> repository) : ApplicationService, IKevinAIChatMessageStore
{
    public async Task AddMessagesAsync(List<ChatHistoryItemDto> chatHistoryItems, CancellationToken cancellationToken)
    {
        var entities = chatHistoryItems.Select(t => new AiChatMessageStore
        {
            Key = t.Key,
            ThreadId = t.ThreadId,
            Timestamp = t.Timestamp,
            Role = t.Role,
            SerializedMessage = t.SerializedMessage,
            MessageText = t.MessageText,
            MessageId = t.MessageId ?? Guid.NewGuid().ToString(),
            IsDeleted = false
        }).ToList();

        foreach (var entity in entities)
        {
            await repository.InsertAsync(entity, cancellationToken: cancellationToken);
        }
    }

    public async Task<List<ChatHistoryItemDto>> GetMessagesAsync(string threadId, CancellationToken cancellationToken)
    {
        var entities = await repository._DbQueryable
            .Where(t => !t.IsDeleted && t.ThreadId == threadId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);
        return entities.Select(t => new ChatHistoryItemDto
        {
            Key = t.Key,
            ThreadId = t.ThreadId,
            Timestamp = t.Timestamp,
            Role = t.Role,
            SerializedMessage = t.SerializedMessage,
            MessageText = t.MessageText,
            MessageId = t.MessageId
        }).ToList();
    }
}
