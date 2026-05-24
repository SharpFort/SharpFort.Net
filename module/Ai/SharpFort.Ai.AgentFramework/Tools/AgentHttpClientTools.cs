using System.ComponentModel;
using System.Net;
using System.Text;

namespace SharpFort.Ai.AgentFramework.Tools;

/// <summary>
/// 给智能体调用的通用 HTTP 工具（支持 GET/POST/PUT/DELETE）
/// </summary>
public static class AgentHttpClientTools
{
    private static HttpClient CreateHttpClient(int timeoutSeconds)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)) };
    }

    private static string BuildUrlWithQuery(string url, IDictionary<string, string>? queryParams)
    {
        if (queryParams is null || queryParams.Count == 0) return url;
        var sb = new StringBuilder();
        foreach (var kv in queryParams)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(Uri.EscapeDataString(kv.Key ?? ""));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kv.Value ?? ""));
        }
        var qs = sb.ToString();
        return url.Contains('?') ? $"{url}&{qs}" : $"{url}?{qs}";
    }

    private static void ApplyHeaders(HttpClient client, IDictionary<string, string>? headers)
    {
        if (headers is null) return;
        foreach (var kv in headers)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                client.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value ?? "");
            }
            catch { }
        }
    }

    [Description("发送 GET 请求")]
    public static async Task<string> GetAsync(
        [Description("目标完整 URL 或相对 URL")] string url,
        [Description("查询参数字典")] IDictionary<string, string>? queryParams = null,
        [Description("自定义请求头字典")] IDictionary<string, string>? headers = null,
        [Description("请求超时（秒）")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullUrl = BuildUrlWithQuery(url, queryParams);
            using var http = CreateHttpClient(timeoutSeconds);
            ApplyHeaders(http, headers);
            using var resp = await http.GetAsync(fullUrl, cancellationToken).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return text ?? string.Empty;
        }
        catch (Exception ex) { return $"❌ 请求失败: {ex.Message}"; }
    }

    [Description("发送 POST 请求")]
    public static async Task<string> PostAsync(
        [Description("目标 URL")] string url,
        [Description("请求体字符串")] string? body = null,
        [Description("Content-Type")] string contentType = "application/json; charset=utf-8",
        [Description("查询参数字典")] IDictionary<string, string>? queryParams = null,
        [Description("自定义请求头字典")] IDictionary<string, string>? headers = null,
        [Description("请求超时（秒）")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullUrl = BuildUrlWithQuery(url, queryParams);
            using var http = CreateHttpClient(timeoutSeconds);
            ApplyHeaders(http, headers);
            using var content = new StringContent(body ?? string.Empty, Encoding.UTF8, contentType.Split(';')[0]);
            using var resp = await http.PostAsync(fullUrl, content, cancellationToken).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return text ?? string.Empty;
        }
        catch (Exception ex) { return $"❌ 请求失败: {ex.Message}"; }
    }

    [Description("发送 PUT 请求")]
    public static async Task<string> PutAsync(
        [Description("目标 URL")] string url,
        [Description("请求体字符串")] string? body = null,
        [Description("Content-Type")] string contentType = "application/json; charset=utf-8",
        [Description("查询参数字典")] IDictionary<string, string>? queryParams = null,
        [Description("自定义请求头字典")] IDictionary<string, string>? headers = null,
        [Description("请求超时（秒）")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullUrl = BuildUrlWithQuery(url, queryParams);
            using var http = CreateHttpClient(timeoutSeconds);
            ApplyHeaders(http, headers);
            using var content = new StringContent(body ?? string.Empty, Encoding.UTF8, contentType.Split(';')[0]);
            using var resp = await http.PutAsync(fullUrl, content, cancellationToken).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return text ?? string.Empty;
        }
        catch (Exception ex) { return $"❌ 请求失败: {ex.Message}"; }
    }

    [Description("发送 DELETE 请求")]
    public static async Task<string> DeleteAsync(
        [Description("目标 URL")] string url,
        [Description("查询参数字典")] IDictionary<string, string>? queryParams = null,
        [Description("自定义请求头字典")] IDictionary<string, string>? headers = null,
        [Description("请求超时（秒）")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullUrl = BuildUrlWithQuery(url, queryParams);
            using var http = CreateHttpClient(timeoutSeconds);
            ApplyHeaders(http, headers);
            using var resp = await http.DeleteAsync(fullUrl, cancellationToken).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return text ?? string.Empty;
        }
        catch (Exception ex) { return $"❌ 请求失败: {ex.Message}"; }
    }
}
