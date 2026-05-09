using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpFort.Ai.Domain.AiGateWay.Exceptions;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Responses;

namespace SharpFort.Ai.Domain.AiGateWay.Impl.ThorCustomOpenAI.Chats;

public class OpenAiResponseService(ILogger<OpenAiResponseService> logger, IHttpClientFactory httpClientFactory) : IOpenAiResponseService
{

    public async IAsyncEnumerable<(string, JsonElement?)> ResponsesStreamAsync(AiModelDescribe options, OpenAiResponsesInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using Activity? openai =
           Activity.Current?.Source.StartActivity("OpenAi 响应");


        HttpClient client = httpClientFactory.CreateClient();

        string endpoint = options.Endpoint.TrimEnd('/');

        //兼容 v1结尾
        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^"/v1".Length];
        }
        string requestUri = endpoint + "/v1/responses";

        HttpResponseMessage response = await client.HttpRequestRaw(requestUri, input, options.ApiKey);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA1848 // Business guard protects this call
            logger.LogError("OpenAI响应异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);
#pragma warning restore CA1848

            throw new InvalidOperationException("OpenAI响应异常" + response.StatusCode);
        }

        using StreamReader stream = new(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;

        string? data = null;
        string eventType = string.Empty;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
#pragma warning disable CA1848, CA1873 // Business guard protects this call
                logger.LogInformation("OpenAI响应异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);
#pragma warning restore CA1848, CA1873

                throw new InvalidOperationException("OpenAI响应异常" + line);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line;
                continue;
            }

            if (!line.StartsWith(OpenAIConstant.Data, StringComparison.Ordinal))
            {
                continue;
            }

            data = line[OpenAIConstant.Data.Length..].Trim();

            JsonElement result = JsonSerializer.Deserialize<JsonElement>(data,
                ThorJsonSerializer.DefaultOptions);

            yield return (eventType, result);
        }
    }

    public async Task<OpenAiResponsesOutput> ResponsesAsync(AiModelDescribe options, OpenAiResponsesInput chatCompletionCreate,
        CancellationToken cancellationToken)
    {
        using Activity? openai =
            Activity.Current?.Source.StartActivity("OpenAI 响应");

        string endpoint = options.Endpoint.TrimEnd('/');

        //兼容 v1结尾
        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^"/v1".Length];
        }
        string requestUri = endpoint + "/v1/responses";

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
            logger.LogError("OpenAI 响应异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}", options.Endpoint,
                response.StatusCode, error);
#pragma warning restore CA1848

            throw new BusinessException("OpenAI响应异常", response.StatusCode.ToString());
        }

        OpenAiResponsesOutput? result =
            await response.Content.ReadFromJsonAsync<OpenAiResponsesOutput>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return result!;
    }
}
