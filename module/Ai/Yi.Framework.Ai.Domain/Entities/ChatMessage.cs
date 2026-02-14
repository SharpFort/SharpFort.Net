using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Ai.Domain.Entities.ValueObjects;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Domain.Entities;

/// <summary>
/// 聊天消息
/// </summary>
[SugarTable("Ai_Message")]
[SugarIndex($"index_{{table}}_{nameof(UserId)}_{nameof(SessionId)}",
    nameof(UserId), OrderByType.Desc,
    nameof(SessionId), OrderByType.Desc
)]
public class ChatMessage : FullAuditedAggregateRoot<Guid>
{
    public ChatMessage()
    {
    }

    public ChatMessage(Guid? userId, Guid? sessionId, string content, string role, string modelId,
        ThorUsageResponse? tokenUsage, Guid? tokenId = null)
    {
        UserId = userId;
        SessionId = sessionId;
        TokenId = tokenId ?? Guid.Empty;
        //如果没有会话，不存储对话内容
        Content = sessionId is null ? null : content;
        Role = role;
        ModelId = modelId;
        if (tokenUsage is not null)
        {
            long inputTokenCount =
                (tokenUsage.PromptTokens.HasValue && tokenUsage.PromptTokens.Value != 0)
                    ? tokenUsage.PromptTokens.Value
                    : (tokenUsage.InputTokens.HasValue && tokenUsage.InputTokens.Value != 0)
                        ? tokenUsage.InputTokens.Value
                        : 0;

            long outputTokenCount =
                (tokenUsage.CompletionTokens.HasValue && tokenUsage.CompletionTokens.Value != 0)
                    ? tokenUsage.CompletionTokens.Value
                    : (tokenUsage.OutputTokens.HasValue && tokenUsage.OutputTokens.Value != 0)
                        ? tokenUsage.OutputTokens.Value
                        : 0;


            this.TokenUsage = new TokenUsageValueObject
            {
                OutputTokenCount = outputTokenCount,
                InputTokenCount = inputTokenCount,
                TotalTokenCount = tokenUsage.TotalTokens ?? 0
            };
        }
        else
        {
            this.TokenUsage = new TokenUsageValueObject
            {
                OutputTokenCount = 0,
                InputTokenCount = 0,
                TotalTokenCount = 0
            };
        }

        this.MessageType = sessionId is null ? MessageTypeEnum.Api : MessageTypeEnum.Web;
    }

    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Token密钥Id（通过API调用时记录，Web调用为Guid.Empty）
    /// </summary>
    public Guid TokenId { get; set; }

    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Content { get; set; }

    public string Role { get; set; }
    public string ModelId { get; set; }
    public string? Remark { get; set; }

    [SugarColumn(IsOwnsOne = true)] 
    public TokenUsageValueObject TokenUsage { get; set; } = new TokenUsageValueObject();

    public MessageTypeEnum MessageType { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool IsHidden { get; set; } = false;
}
