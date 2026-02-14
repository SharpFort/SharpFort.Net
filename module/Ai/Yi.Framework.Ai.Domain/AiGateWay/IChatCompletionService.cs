using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public interface IChatCompletionService
{
    /// <summary>
    /// 聊天完成-流式
    /// </summary>
    /// <param name="aiModelDescribe"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ThorChatCompletionsResponse> CompleteChatStreamAsync(AiModelDescribe aiModelDescribe,
        ThorChatCompletionsRequest input,
        CancellationToken cancellationToken);

    /// <summary>
    /// 聊天完成-非流式
    /// </summary>
    /// <param name="aiModelDescribe"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ThorChatCompletionsResponse> CompleteChatAsync(AiModelDescribe aiModelDescribe,
        ThorChatCompletionsRequest input,
        CancellationToken cancellationToken);
}
