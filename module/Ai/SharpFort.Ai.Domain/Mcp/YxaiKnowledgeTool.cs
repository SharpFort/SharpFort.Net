using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Attributes;

namespace SharpFort.Ai.Domain.Mcp;

[SfAgentTool]
public class YxaiKnowledgeTool : ISingletonDependency
{
    private static readonly CompositeFormat ContentUrlFormat = CompositeFormat.Parse("https://ccnetcore.com/prod-api/article/{0}");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YxaiKnowledgeTool> _logger;

    private const string DirectoryUrl =
        "https://ccnetcore.com/prod-api/article/all/discuss-id/3a1efdde-dbff-aa86-d843-00278a8c1839";

    public YxaiKnowledgeTool(
        IHttpClientFactory httpClientFactory,
        ILogger<YxaiKnowledgeTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [SfAgentTool("意心Ai平台知识库"), DisplayName("YxaiKnowledge"),
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
#pragma warning disable CA1848 // Business guard protects this call
                _logger.LogError("意心知识库目录接口调用失败: {StatusCode}", directoryResponse.StatusCode);
#pragma warning restore CA1848
                return [];
            }

            var directoryJson = await directoryResponse.Content.ReadAsStringAsync();
            var directories = JsonSerializer.Deserialize(directoryJson,
                YxaiKnowledgeJsonContext.Default.ListYxaiKnowledgeDirectoryItem);

            if (directories == null || directories.Count == 0)
            {
                return [];
            }

            // 2. 循环调用内容接口获取每个目录的内容
            var result = new List<YxaiKnowledgeItem>();
            foreach (var directory in directories)
            {
                try
                {
                    var contentUrl = string.Format(CultureInfo.InvariantCulture, ContentUrlFormat, directory.Id);
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
#pragma warning disable CA1848 // Business guard protects this call
                        _logger.LogWarning("获取知识库内容失败: {StatusCode}, DirectoryId: {DirectoryId}",
                            contentResponse.StatusCode, directory.Id);
#pragma warning restore CA1848
                        result.Add(new YxaiKnowledgeItem
                        {
                            Name = directory.Name,
                            Content = $"获取内容失败: {contentResponse.StatusCode}"
                        });
                    }
                }
                catch (Exception ex)
                {
#pragma warning disable CA1848 // Business guard protects this call (catch block)
                    _logger.LogError(ex, "获取知识库内容发生异常, DirectoryId: {DirectoryId}", directory.Id);
#pragma warning restore CA1848
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
#pragma warning disable CA1848 // Business guard protects this call (catch block)
            _logger.LogError(ex, "获取意心知识库发生异常");
#pragma warning restore CA1848
            return [];
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
internal sealed partial class YxaiKnowledgeJsonContext : JsonSerializerContext
{
}

#endregion
