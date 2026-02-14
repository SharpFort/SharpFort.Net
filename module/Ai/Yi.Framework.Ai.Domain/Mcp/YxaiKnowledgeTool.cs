using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Yi.Framework.Ai.Domain.Shared.Attributes;

namespace Yi.Framework.Ai.Domain.Mcp;

[YiAgentTool]
public class YxaiKnowledgeTool : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YxaiKnowledgeTool> _logger;

    private const string DirectoryUrl =
        "https://ccnetcore.com/prod-api/article/all/discuss-id/3a1efdde-dbff-aa86-d843-00278a8c1839";

    private const string ContentUrlTemplate = "https://ccnetcore.com/prod-api/article/{0}";

    public YxaiKnowledgeTool(
        IHttpClientFactory httpClientFactory,
        ILogger<YxaiKnowledgeTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [YiAgentTool("意心Ai平台知识库"), DisplayName("YxaiKnowledge"),
     Description("获取意心AI相关内容的知识库目录及内容列表")]
    public async Task<List<YxaiKnowledgeItem>> YxaiKnowledge()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            // 1. 先获取目录列表
            var directoryResponse = await client.GetAsync(DirectoryUrl);
            if (!directoryResponse.IsSuccessStatusCode)
            {
                _logger.LogError("意心知识库目录接口调用失败: {StatusCode}", directoryResponse.StatusCode);
                return new List<YxaiKnowledgeItem>();
            }

            var directoryJson = await directoryResponse.Content.ReadAsStringAsync();
            var directories = JsonSerializer.Deserialize(directoryJson,
                YxaiKnowledgeJsonContext.Default.ListYxaiKnowledgeDirectoryItem);

            if (directories == null || directories.Count == 0)
            {
                return new List<YxaiKnowledgeItem>();
            }

            // 2. 循环调用内容接口获取每个目录的内容
            var result = new List<YxaiKnowledgeItem>();
            foreach (var directory in directories)
            {
                try
                {
                    var contentUrl = string.Format(ContentUrlTemplate, directory.Id);
                    var contentResponse = await client.GetAsync(contentUrl);

                    if (contentResponse.IsSuccessStatusCode)
                    {
                        var contentJson = await contentResponse.Content.ReadAsStringAsync();
                        var contentResult = JsonSerializer.Deserialize(contentJson,
                            YxaiKnowledgeJsonContext.Default.YxaiKnowledgeContentResponse);

                        result.Add(new YxaiKnowledgeItem
                        {
                            Name = directory.Name,
                            Content = contentResult?.Content ?? ""
                        });
                    }
                    else
                    {
                        _logger.LogWarning("获取知识库内容失败: {StatusCode}, DirectoryId: {DirectoryId}",
                            contentResponse.StatusCode, directory.Id);
                        result.Add(new YxaiKnowledgeItem
                        {
                            Name = directory.Name,
                            Content = $"获取内容失败: {contentResponse.StatusCode}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取知识库内容发生异常, DirectoryId: {DirectoryId}", directory.Id);
                    result.Add(new YxaiKnowledgeItem
                    {
                        Name = directory.Name,
                        Content = "获取内容发生异常"
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取意心知识库发生异常");
            return new List<YxaiKnowledgeItem>();
        }
    }
}

#region DTO

public class YxaiKnowledgeDirectoryItem
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";

    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class YxaiKnowledgeContentResponse
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}

/// <summary>
/// 合并后的知识库项，包含目录和内容
/// </summary>
public class YxaiKnowledgeItem
{
    /// <summary>
    /// 目录名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 知识库内容
    /// </summary>
    public string Content { get; set; } = "";
}

#endregion

#region JSON 序列化上下文

[JsonSerializable(typeof(List<YxaiKnowledgeDirectoryItem>))]
[JsonSerializable(typeof(YxaiKnowledgeContentResponse))]
internal partial class YxaiKnowledgeJsonContext : JsonSerializerContext
{
}

#endregion
