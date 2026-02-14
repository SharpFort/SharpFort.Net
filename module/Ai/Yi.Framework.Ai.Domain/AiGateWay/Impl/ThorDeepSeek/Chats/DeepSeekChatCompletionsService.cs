using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yi.Framework.Ai.Domain.AiGateWay.Exceptions;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorDeepSeek.Chats;

public sealed class DeepSeekChatCompletionsService(ILogger<DeepSeekChatCompletionsService> logger,IHttpClientFactory httpClientFactory)
    : IChatCompletionService
{
    public async IAsyncEnumerable<ThorChatCompletionsResponse> CompleteChatStreamAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            options.Endpoint = "https://api.deepseek.com/v1";
        }

        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话流式补全");
        
        var endpoint = options?.Endpoint.TrimEnd('/');
        //兼容 v1结尾
        if (endpoint != null && endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint.Substring(0, endpoint.Length - "/v1".Length);
        }
        var requestUri = endpoint + "/v1/chat/completions";
        
        var response = await httpClientFactory.CreateClient().HttpRequestRaw(
            requestUri,
            chatCompletionCreate, options.ApiKey);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.PaymentRequired)
        {
            throw new PaymentRequiredException();
        }

        // 如果限流则抛出限流异常
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ThorRateLimitException();
        }

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            logger.LogError("OpenAI对话异常 , StatusCode: {StatusCode} ", response.StatusCode);

            throw new BusinessException("OpenAI对话异常", response.StatusCode.ToString());
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;
        var first = true;
        var isThink = false;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);

                throw new BusinessException("OpenAI对话异常", line);
            }

            if (line.StartsWith(OpenAIConstant.Data))
                line = line[OpenAIConstant.Data.Length..];

            line = line.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line == OpenAIConstant.Done)
            {
                break;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }


            var result = JsonSerializer.Deserialize<ThorChatCompletionsResponse>(line,
                ThorJsonSerializer.DefaultOptions);
            yield return result;
        }
    }

    public async Task<ThorChatCompletionsResponse> CompleteChatAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        CancellationToken cancellationToken)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话补全");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            options.Endpoint = "https://api.deepseek.com/v1";
        }

        var endpoint = options?.Endpoint.TrimEnd('/');
        //兼容 v1结尾
        if (endpoint != null && endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint.Substring(0, endpoint.Length - "/v1".Length);
        }
        var requestUri = endpoint + "/v1/chat/completions";
        
        var response = await httpClientFactory.CreateClient().PostJsonAsync(
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
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode, error);

            throw new BusinessException("OpenAI对话异常", response.StatusCode.ToString());
        }

        var result =
            await response.Content.ReadFromJsonAsync<ThorChatCompletionsResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }
}
