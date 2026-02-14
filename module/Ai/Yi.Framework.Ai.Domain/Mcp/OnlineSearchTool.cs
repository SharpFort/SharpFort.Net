using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Yi.Framework.Ai.Domain.Shared.Attributes;

namespace Yi.Framework.Ai.Domain.Mcp;

[YiAgentTool]
public class OnlineSearchTool : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OnlineSearchTool> _logger;
    private readonly string _baiduApiKey;
    private const string BaiduSearchUrl = "https://qianfan.baidubce.com/v2/ai_search/web_search";

    public OnlineSearchTool(
        IHttpClientFactory httpClientFactory,
        ILogger<OnlineSearchTool> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _baiduApiKey = configuration["BaiduSearch:ApiKey"] ?? "";
    }

    [YiAgentTool("联网搜索"), DisplayName("OnlineSearch"), Description("进行在线搜索，获取最新的网络信息，近期信息是7天，实时信息是1天")]
    public async Task<string> OnlineSearch([Description("搜索关键字")] string keyword,
        [Description("距离现在多久天")] int? daysAgo = null)
    {
        if (daysAgo <= 0)
        {
            daysAgo = 1;
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return "搜索关键词不能为空";
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            // 构建请求体
            var requestBody = new BaiduSearchRequest
            {
                Messages = new List<BaiduSearchMessage>
                {
                    new() { Role = "user", Content = keyword }
                }
            };

            // 设置时间范围过滤
            if (daysAgo.HasValue)
            {
                requestBody.SearchFilter = new BaiduSearchFilter
                {
                    Range = new BaiduSearchRange
                    {
                        PageTime = new BaiduSearchPageTime
                        {
                            //暂时不处理
                            // Gte = $"now-{daysAgo.Value}d/d",
                            Gte = "now-1w/d"
                        }
                    }
                };
            }

            var jsonContent = JsonSerializer.Serialize(requestBody, BaiduJsonContext.Default.BaiduSearchRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 设置请求头
            var request = new HttpRequestMessage(HttpMethod.Post, BaiduSearchUrl)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {_baiduApiKey}");

            // 发送请求
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("百度搜索接口调用失败: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return $"搜索失败: {response.StatusCode}";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize(responseJson, BaiduJsonContext.Default.BaiduSearchResponse);

            if (searchResult?.References == null || searchResult.References.Count == 0)
            {
                return "未找到相关搜索结果";
            }

            // 格式化搜索结果返回给AI
            return FormatSearchResults(searchResult.References);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "百度搜索网络请求异常");
            return "搜索服务暂时不可用，请稍后重试";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "百度搜索请求超时");
            return "搜索请求超时，请稍后重试";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "百度搜索结果解析失败");
            return "搜索结果解析失败";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "百度搜索发生未知异常");
            return "搜索发生异常，请稍后重试";
        }
    }

    /// <summary>
    /// 格式化搜索结果
    /// </summary>
    private string FormatSearchResults(List<BaiduSearchReference> references)
    {
        var sb = new StringBuilder();
        sb.AppendLine("搜索结果：");
        sb.AppendLine();

        var count = 0;
        foreach (var item in references.Take(10)) // 最多返回10条
        {
            count++;
            sb.AppendLine($"【{count}】{item.Title}");
            sb.AppendLine($"来源：{item.Website} | 时间：{item.Date}");
            sb.AppendLine($"摘要：{item.Snippet}");
            sb.AppendLine($"链接：{item.Url}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

#region 百度搜索 DTO

/// <summary>
/// 百度搜索请求
/// </summary>
public class BaiduSearchRequest
{
    [JsonPropertyName("messages")] public List<BaiduSearchMessage> Messages { get; set; } = new();

    [JsonPropertyName("search_filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BaiduSearchFilter? SearchFilter { get; set; }
}

/// <summary>
/// 百度搜索过滤器
/// </summary>
public class BaiduSearchFilter
{
    [JsonPropertyName("range")] public BaiduSearchRange? Range { get; set; }
}

/// <summary>
/// 百度搜索范围
/// </summary>
public class BaiduSearchRange
{
    [JsonPropertyName("page_time")] public BaiduSearchPageTime? PageTime { get; set; }
}

/// <summary>
/// 百度搜索时间范围
/// </summary>
public class BaiduSearchPageTime
{
    [JsonPropertyName("gte")] public string? Gte { get; set; }

    // [JsonPropertyName("lte")] public string? Lte { get; set; }
}

/// <summary>
/// 百度搜索消息
/// </summary>
public class BaiduSearchMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";

    [JsonPropertyName("content")] public string Content { get; set; } = "";
}

/// <summary>
/// 百度搜索响应
/// </summary>
public class BaiduSearchResponse
{
    [JsonPropertyName("request_id")] public string? RequestId { get; set; }

    [JsonPropertyName("references")] public List<BaiduSearchReference>? References { get; set; }
}

/// <summary>
/// 百度搜索结果项
/// </summary>
public class BaiduSearchReference
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("title")] public string? Title { get; set; }

    [JsonPropertyName("date")] public string? Date { get; set; }

    [JsonPropertyName("snippet")] public string? Snippet { get; set; }

    [JsonPropertyName("website")] public string? Website { get; set; }
}

#endregion

#region JSON 序列化上下文

[JsonSerializable(typeof(BaiduSearchRequest))]
[JsonSerializable(typeof(BaiduSearchResponse))]
[JsonSerializable(typeof(BaiduSearchFilter))]
[JsonSerializable(typeof(BaiduSearchRange))]
[JsonSerializable(typeof(BaiduSearchPageTime))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class BaiduJsonContext : JsonSerializerContext
{
}

#endregion
