using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpFort.Ai.Domain.AiGateWay.Exceptions;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

namespace SharpFort.Ai.Domain.AiGateWay.Impl.ThorCustomOpenAI.Chats;

public sealed class OpenAiChatCompletionsService(
    ILogger<OpenAiChatCompletionsService> logger,
    IHttpClientFactory httpClientFactory)
    : IChatCompletionService
{
    public async IAsyncEnumerable<ThorChatCompletionsResponse> CompleteChatStreamAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using Activity? openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话流式补全");

        string endpoint = options.Endpoint.TrimEnd('/');

        //兼容 v1结尾
        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^"/v1".Length];
        }

        string requestUri = endpoint + "/v1/chat/completions";

        HttpResponseMessage response = await httpClientFactory.CreateClient().HttpRequestRaw(
            requestUri,
            chatCompletionCreate, options.ApiKey);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        // 如果限流则抛出限流异常
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ThorRateLimitException();
        }

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
#pragma warning disable CA1848 // Business guard protects this call
            logger.LogError("OpenAI对话异常 , StatusCode: {StatusCode} 错误响应内容：{Content}", response.StatusCode,
                error);
#pragma warning restore CA1848

            throw new BusinessException("OpenAI对话异常：" + error, response.StatusCode.ToString());
        }

        using StreamReader stream = new(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;
        bool first = true;
        bool isThink = false;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
#pragma warning disable CA1848, CA1873 // Business guard protects this call
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);
#pragma warning restore CA1848, CA1873

                throw new BusinessException("OpenAI对话异常", line);
            }

            if (line.StartsWith(OpenAIConstant.Data, StringComparison.Ordinal))
            {
                line = line[OpenAIConstant.Data.Length..];
            }

            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line == OpenAIConstant.Done)
            {
                break;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }


            ThorChatCompletionsResponse? result = JsonSerializer.Deserialize<ThorChatCompletionsResponse>(line,
                ThorJsonSerializer.DefaultOptions);

            if (result == null)
            {
                continue;
            }

            ThorChatMessage? content = result?.Choices?.FirstOrDefault()?.Delta;

            if (first && content?.Content == OpenAIConstant.ThinkStart)
            {
                isThink = true;
                continue;
                // 需要将content的内容转换到其他字段
            }

            if (isThink && content?.Content?.Contains(OpenAIConstant.ThinkEnd) == true)
            {
                isThink = false;
                // 需要将content的内容转换到其他字段
                continue;
            }

            if (isThink && result?.Choices != null)
            {
                // 需要将content的内容转换到其他字段
                foreach (ThorChatChoiceResponse choice in result.Choices)
                {
                    choice.Delta.ReasoningContent = choice.Delta.Content;
                    choice.Delta.Content = string.Empty;
                }
            }

            first = false;

            yield return result!;
        }
    }

    public async Task<ThorChatCompletionsResponse> CompleteChatAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        CancellationToken cancellationToken)
    {
        using Activity? openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话补全");

        string endpoint = options.Endpoint.TrimEnd('/');

        //兼容 v1结尾
        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^"/v1".Length];
        }
        string requestUri = endpoint + "/v1/chat/completions";
        HttpResponseMessage response = await httpClientFactory.CreateClient().PostJsonAsync(
            requestUri,
            chatCompletionCreate, options.ApiKey).ConfigureAwait(false);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new BusinessException("渠道未登录,请联系管理人员", "401");
        }

        // 如果限流则抛出限流异常
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ThorRateLimitException();
        }

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA1848 // Business guard protects this call
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);
#pragma warning restore CA1848

            throw new BusinessException("OpenAI对话异常", response.StatusCode.ToString());
        }

        ThorChatCompletionsResponse? result =
            await response.Content.ReadFromJsonAsync<ThorChatCompletionsResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return result!;
    }
}
