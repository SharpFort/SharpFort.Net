using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yi.Framework.Ai.Domain.AiGateWay.Exceptions;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Responses;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorGemini.Chats;

public class GeminiGenerateContentService(
    ILogger<GeminiGenerateContentService> logger,
    IHttpClientFactory httpClientFactory) : IGeminiGenerateContentService
{
    public async IAsyncEnumerable<JsonElement?> GenerateContentStreamAsync(AiModelDescribe options, JsonElement input,
        CancellationToken cancellationToken)
    {
        var response = await httpClientFactory.CreateClient().PostJsonAsync(
            options?.Endpoint.TrimEnd('/') + $"/v1beta/models/{options.ModelId}:streamGenerateContent?alt=sse",
            input, null, new Dictionary<string, string>()
            {
                { "x-goog-api-key", options.ApiKey }
            }).ConfigureAwait(false);
        

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("Gemini生成异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);

            throw new Exception("Gemini生成异常" + response.StatusCode);
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith(OpenAIConstant.Data)) continue;

            var data = line[OpenAIConstant.Data.Length..].Trim();

            var result = JsonSerializer.Deserialize<JsonElement>(data,
                ThorJsonSerializer.DefaultOptions);

            yield return result;
        }
    }

    public async Task<JsonElement> GenerateContentAsync(AiModelDescribe options, JsonElement input,
        CancellationToken cancellationToken)
    {
        var response = await httpClientFactory.CreateClient().PostJsonAsync(
            options?.Endpoint.TrimEnd('/') + $"/v1beta/models/{options.ModelId}:generateContent",
            input, null, new Dictionary<string, string>()
            {
                { "x-goog-api-key", options.ApiKey }
            }).ConfigureAwait(false);

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
            logger.LogError("Gemini 生成异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);

            throw new BusinessException("Gemini 生成异常", response.StatusCode.ToString());
        }

        var result =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }
}
