using System.Text.Json;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Responses;

namespace SharpFort.Ai.Domain.AiGateWay;

public interface IOpenAiResponseService
{
    /// <summary>
    /// 响应-流式
    /// </summary>
    /// <param name="aiModelDescribe"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<(string, JsonElement?)> ResponsesStreamAsync(AiModelDescribe aiModelDescribe,
        OpenAiResponsesInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// 响应-非流式
    /// </summary>
    /// <param name="aiModelDescribe"></param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<OpenAiResponsesOutput> ResponsesAsync(AiModelDescribe aiModelDescribe,
        OpenAiResponsesInput input,
        CancellationToken cancellationToken);
}
