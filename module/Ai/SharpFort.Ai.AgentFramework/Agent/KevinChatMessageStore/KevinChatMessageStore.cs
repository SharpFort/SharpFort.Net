using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SharpFort.Ai.AgentFramework.Dto;
using SharpFort.Ai.AgentFramework.Interfaces;
using System.Text.Json;

namespace SharpFort.Ai.AgentFramework.Agent;

public sealed class KevinChatMessageStore : ChatHistoryProvider
{
    private readonly IKevinAIChatMessageStore _chatMessageStore;
    public string ThreadDbKey { get; }

    public KevinChatMessageStore(IKevinAIChatMessageStore vectorStore, string aiChatsId)
    {
        _chatMessageStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        ThreadDbKey = aiChatsId;
        JsonSerializer.SerializeToElement(ThreadDbKey);
    }

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var data = await _chatMessageStore.GetMessagesAsync(ThreadDbKey, cancellationToken);
        var messages = data.OrderByDescending(t => t.Timestamp)
            .ToList()
            .ConvertAll(x => JsonSerializer.Deserialize<ChatMessage>(x.SerializedMessage!)!);
        messages.Reverse();
        if (context.RequestMessages.Any())
        {
            foreach (var item in context.RequestMessages)
            {
                item.CreatedAt ??= DateTimeOffset.UtcNow;
            }
        }
        return messages;
    }

    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        var responseMessages = context.ResponseMessages ?? Array.Empty<ChatMessage>();
        var allNewMessages = context.RequestMessages.Concat(responseMessages).ToList();
        var toolsMessages = allNewMessages.Where(x => x.Role == ChatRole.Tool)
            .OrderBy(t => t.CreatedAt).ToList();
        var toolsMessagesI = 0;

        if (toolsMessages.Count > 0)
        {
            foreach (var item in allNewMessages)
            {
                if (item.Role == ChatRole.Assistant)
                {
                    if (string.IsNullOrEmpty(item.Text))
                    {
                        toolsMessages[toolsMessagesI].CreatedAt = item.CreatedAt!.Value.AddMilliseconds(1);
                        toolsMessagesI++;
                    }
                }
            }
        }

        if (allNewMessages.Count > 0)
        {
            var addData = allNewMessages.Select(x => new ChatHistoryItemDto
            {
                Key = ThreadDbKey + x.MessageId,
                Timestamp = x.CreatedAt,
                ThreadId = ThreadDbKey,
                MessageId = x.MessageId,
                Role = x.Role.Value,
                SerializedMessage = JsonSerializer.Serialize(x),
                MessageText = x.Text
            }).ToList();

            await _chatMessageStore.AddMessagesAsync(addData, cancellationToken);
        }
    }
}
