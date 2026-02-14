using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public interface IAnthropicChatCompletionService
{
    /// <summary>
    /// 非流式对话补全
    /// </summary>
    /// <param name="request">对话补全请求参数对象</param>
    /// <param name="aiModelDescribe">平台参数对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<AnthropicChatCompletionDto> ChatCompletionsAsync(AiModelDescribe aiModelDescribe,
        AnthropicInput request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 流式对话补全
    /// </summary>
    /// <param name="request">对话补全请求参数对象</param>
    /// <param name="aiModelDescribe">平台参数对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    IAsyncEnumerable<(string, AnthropicStreamDto?)> StreamChatCompletionsAsync(AiModelDescribe aiModelDescribe,
        AnthropicInput request,
        CancellationToken cancellationToken = default);
}
