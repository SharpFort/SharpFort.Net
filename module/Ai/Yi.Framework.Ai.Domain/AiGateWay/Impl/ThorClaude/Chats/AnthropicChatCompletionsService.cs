using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorClaude.Chats;

public class AnthropicChatCompletionsService(
    IHttpClientFactory httpClientFactory,
    ILogger<AnthropicChatCompletionsService> logger)
    : IAnthropicChatCompletionService
{
    public async Task<AnthropicChatCompletionDto> ChatCompletionsAsync(AiModelDescribe options, AnthropicInput input,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("Claudia 对话补全");

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            options.Endpoint = "https://api.anthropic.com/";
        }

        var client = httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>
        {
            { "x-api-key", options.ApiKey },
            { "authorization", "Bearer " + options.ApiKey },
            { "anthropic-version", "2023-06-01" }
        };


        bool isThink = input.Model.EndsWith("-thinking");
        input.Model = input.Model.Replace("-thinking", string.Empty);

        if (input.MaxTokens is < 2048)
        {
            input.MaxTokens = 2048;
        }

        if (isThink && input.Thinking is null)
        {
            input.Thinking = new AnthropicThinkingInput()
            {
                Type = "enabled",
                BudgetTokens = 4000
            };
        }

        if (input.Thinking is not null && input.Thinking.BudgetTokens > 0 && input.MaxTokens != null)
        {
            if (input.Thinking.BudgetTokens > input.MaxTokens)
            {
                input.Thinking.BudgetTokens = input.MaxTokens.Value - 1;
                if (input.Thinking.BudgetTokens > 63999)
                {
                    input.Thinking.BudgetTokens = 63999;
                }
            }
        }

        var response =
            await client.PostJsonAsync(options.Endpoint.TrimEnd('/') + "/v1/messages", input, string.Empty, headers);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            Guid errorId = Guid.NewGuid();
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var message = $"恭喜你运气爆棚遇到了错误，对话异常：StatusCode【{response.StatusCode.GetHashCode()}】，ErrorId【{errorId}】";
            if (error.Contains("prompt is too long") || error.Contains("提示词太长")||error.Contains("input tokens exceeds the model's maximum context length"))
            {
                message += $", tip: 当前提示词过长，上下文已达到上限，如在 claudecode中使用，建议执行/compact压缩当前会话，或开启新会话后重试";
            }

            logger.LogError(
                $"Anthropic非流式对话异常 请求地址：{options.Endpoint},ErrorId：{errorId}, StatusCode: {response.StatusCode.GetHashCode()}, Response: {error}");
            throw new Exception(message);
        }

        var value =
            await response.Content.ReadFromJsonAsync<AnthropicChatCompletionDto>(ThorJsonSerializer.DefaultOptions,
                cancellationToken: cancellationToken);

        return value;
    }

    public async IAsyncEnumerable<(string, AnthropicStreamDto?)> StreamChatCompletionsAsync(AiModelDescribe options,
        AnthropicInput input,
        CancellationToken cancellationToken = default)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("Claudia 对话补全");

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            options.Endpoint = "https://api.anthropic.com/";
        }

        var client = httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>
        {
            { "x-api-key", options.ApiKey },
            { "authorization", options.ApiKey },
            { "anthropic-version", "2023-06-01" }
        };

        var response = await client.HttpRequestRaw(options.Endpoint.TrimEnd('/') + "/v1/messages", input, string.Empty,
            headers);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            Guid errorId = Guid.NewGuid();
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var message = $"恭喜你运气爆棚遇到了错误，对话异常：StatusCode【{response.StatusCode.GetHashCode()}】，ErrorId【{errorId}】";
            if (error.Contains("prompt is too long") || error.Contains("提示词太长")||error.Contains("input tokens exceeds the model's maximum context length"))
            {
                message += $", tip: 当前提示词过长，上下文已达到上限，如在 claudecode中使用，建议执行/compact压缩当前会话，或开启新会话后重试";
            }

            logger.LogError(
                $"Anthropic流式对话异常 请求地址：{options.Endpoint},ErrorId：{errorId}, StatusCode: {response.StatusCode.GetHashCode()}, Response: {error}");

            throw new Exception(message);
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;

        string? data = null;
        string eventType = string.Empty;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);

                throw new Exception("OpenAI对话异常" + line);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("event:"))
            {
                eventType = line;
                continue;
            }

            if (!line.StartsWith(OpenAIConstant.Data)) continue;

            data = line[OpenAIConstant.Data.Length..].Trim();

            // 处理流结束标记
            if (data == "[DONE]")
            {
                break;
            }

            var result = JsonSerializer.Deserialize<AnthropicStreamDto>(data,
                ThorJsonSerializer.DefaultOptions);

            yield return (eventType, result);
        }
    }
}
