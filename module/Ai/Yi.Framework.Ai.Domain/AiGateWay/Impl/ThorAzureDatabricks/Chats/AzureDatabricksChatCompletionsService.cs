using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yi.Framework.Ai.Domain.AiGateWay.Exceptions;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorAzureDatabricks.Chats;

public class AzureDatabricksChatCompletionsService(ILogger<AzureDatabricksChatCompletionsService> logger,IHttpClientFactory httpClientFactory)
    : IChatCompletionService
{
    private string GetAddress(AiModelDescribe? options, string model)
    {
        // This method should return the appropriate URL for the Azure Databricks API
        // based on the provided options and model.
        // For now, we will return a placeholder URL.
        return $"{options?.Endpoint.TrimEnd('/')}/serving-endpoints/{model}/invocations";
    }

    public async IAsyncEnumerable<ThorChatCompletionsResponse> CompleteChatStreamAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var address = GetAddress(options, chatCompletionCreate.Model);
        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话流式补全");

        chatCompletionCreate.StreamOptions = null;

        var response = await httpClientFactory.CreateClient().HttpRequestRaw(
            address,
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
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("OpenAI对话异常 , StatusCode: {StatusCode} 错误响应内容：{Content}", response.StatusCode,
                error);

            throw new BusinessException(response.StatusCode.ToString(), "OpenAI对话异常：" + error);
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;
        var first = true;
        var isThink = false;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);

                throw new BusinessException("500", "OpenAI对话异常", line);
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

            if (result == null)
            {
                continue;
            }
            yield return result;
        }
    }

    public async Task<ThorChatCompletionsResponse> CompleteChatAsync(AiModelDescribe options,
        ThorChatCompletionsRequest chatCompletionCreate,
        CancellationToken cancellationToken)
    {
        var address = GetAddress(options, chatCompletionCreate.Model);

        using var openai =
            Activity.Current?.Source.StartActivity("OpenAI 对话补全");

        var response = await httpClientFactory.CreateClient().PostJsonAsync(
            address,
            chatCompletionCreate, options.ApiKey).ConfigureAwait(false);

        openai?.SetTag("Model", chatCompletionCreate.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new BusinessException("401", "渠道未登录,请联系管理人员");
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
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);

            throw new BusinessException(response.StatusCode.ToString(), "OpenAI对话异常");
        }

        var result =
            await response.Content.ReadFromJsonAsync<ThorChatCompletionsResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }
}
