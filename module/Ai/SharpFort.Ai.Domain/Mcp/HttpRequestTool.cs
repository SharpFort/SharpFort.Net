using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Attributes;

namespace SharpFort.Ai.Domain.Mcp;

[SfAgentTool]
public class HttpRequestTool(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpRequestTool> logger) : ISingletonDependency
{
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<HttpRequestTool> _logger = logger;

    [SfAgentTool("HTTP请求"), DisplayName("HttpRequest"),
     Description("发送HTTP请求，支持GET/POST/PUT/DELETE等方法，获取指定URL的响应内容")]
    public async Task<string> HttpRequest(
        [Description("请求的URL地址")] string url,
        [Description("HTTP方法：GET、POST、PUT、DELETE等")] string method = "GET",
        [Description("请求体内容（JSON字符串），POST/PUT时使用")] string? body = null,
        [Description("请求头，格式：key1:value1,key2:value2")] string? headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "URL不能为空";
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            method = "GET";
        }

        try
        {
            HttpClient client = _httpClientFactory.CreateClient();
            HttpRequestMessage request = new(new HttpMethod(method.ToUpper(CultureInfo.InvariantCulture)), url);

            // 添加请求体
            if (!string.IsNullOrWhiteSpace(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            // 添加自定义请求头
            if (!string.IsNullOrWhiteSpace(headers))
            {
                AddHeaders(request, headers);
            }

            HttpResponseMessage response = await client.SendAsync(request);
            return await FormatResponse(response);
        }
        catch (Exception ex)
        {
#pragma warning disable CA1848 // Business guard protects this call (catch block)
            _logger.LogError(ex, "HTTP {Method}请求失败: {Url}", method, url);
#pragma warning restore CA1848
            return $"请求失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 添加请求头
    /// </summary>
    private static void AddHeaders(HttpRequestMessage request, string headers)
    {
        string[] headerPairs = headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string pair in headerPairs)
        {
            string[] parts = pair.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                request.Headers.TryAddWithoutValidation(parts[0], parts[1]);
            }
        }
    }

    /// <summary>
    /// 格式化响应结果
    /// </summary>
    private static async Task<string> FormatResponse(HttpResponseMessage response)
    {
        StringBuilder sb = new();
        sb.AppendLine(CultureInfo.InvariantCulture, $"状态码: {(int)response.StatusCode} {response.StatusCode}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Content-Type: {response.Content.Headers.ContentType?.ToString() ?? "未知"}");
        sb.AppendLine();

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            sb.AppendLine("响应内容为空");
        }
        else
        {
            // 尝试格式化JSON
            if (IsJsonContentType(response.Content.Headers.ContentType?.MediaType))
            {
                try
                {
                    JsonDocument jsonDoc = JsonDocument.Parse(content);
                    sb.AppendLine("响应内容（JSON格式化）：");
                    sb.AppendLine(JsonSerializer.Serialize(jsonDoc, s_indentedOptions));
                }
                catch
                {
                    sb.AppendLine("响应内容：");
                    sb.AppendLine(content);
                }
            }
            else
            {
                sb.AppendLine("响应内容：");
                sb.AppendLine(content);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 判断是否为JSON内容类型
    /// </summary>
    private static bool IsJsonContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
            && (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase));
    }
}
