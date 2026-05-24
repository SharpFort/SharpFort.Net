using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SharpFort.Ai.AgentFramework.Interfaces;

namespace SharpFort.Ai.AgentFramework.Tools;

public class HttpClientFunction
{
    private readonly IAIAgentService _aiAgentService;

    private readonly Dictionary<string, string> _seoTemplates = new()
    {
        { "https://cn.bing.com/search?q={0}", "必应搜索" },
        { "https://www.baidu.com/s?wd={0}&ie=utf-8&rn=10", "百度搜索" },
        { "https://kaifa.baidu.com/searchPage?wd={0}&ie=utf-8&rn=10", "百度开发搜索" },
        { "https://www.sogou.com/web?query={0}", "搜狗搜索" },
        { "https://www.so.com/s?q={0}", "360搜索" },
    };

    private const string SystemTemplate = @"
角色：你是一款专业的搜索引擎助手，专门负责将HTML内容转换为结构化的Markdown格式。
能力：HTML解析、信息提取与总结、智能过滤、格式标准化。
重要规则：
1. 只使用互联网查询信息来回答
2. 如果互联网查询信息中没有相关信息，请明确告知用户
3. 不要编造或推测
4. 回答要清晰、准确、有条理
5. 可以引用信息来源";

    public HttpClientFunction(IAIAgentService aiAgentService)
    {
        _aiAgentService = aiAgentService;
    }

    public async Task<string> GetSeoAsync(string value, string aiEndPoint, string aiModelName, string aiModelKey)
    {
        var htmls = new List<string>();
        string result = "";

        foreach (var template in _seoTemplates)
        {
            try
            {
                var html = await GetUrlSeoAsync(string.Format(template.Key, value));
                html = CleanHtml(html);
                htmls.Add(html + "\n 信息来源于: " + template.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        result = (await _aiAgentService.CreateOpenAIAgentAndSendMSG(new AISetting
        {
            AIUrl = aiEndPoint,
            AIKeySecret = aiModelKey,
            AIDefaultModel = aiModelName,
            IsStreame = false,
        }, new ChatClientAgentOptions
        {
            Name = "搜索引擎助手",
            Description = SystemTemplate,
            ChatOptions = new ChatOptions
            {
                Temperature = 0.3f,
                ResponseFormat = ChatResponseFormat.Text,
                Instructions = SystemTemplate
            },
        }, $"用户搜索意图:{value}\n互联网搜索信息如下：" + string.Join("\n", htmls) + "\n请你根据用户搜索意图进行提取总结")).Item2;

        return "互联网查询信息:\n" + (result ?? "无相关信息");
    }

    public async Task<string> GetUrlSeoAsync(string url)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
        };

        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        http.DefaultRequestHeaders.Add("Referer", "https://www.baidu.com/");
        http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        http.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

        try
        {
            var response = await http.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] byteResult = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Encoding encoding = null!;
            string charset = response.Content.Headers.ContentType?.CharSet ?? "";
            if (!string.IsNullOrEmpty(charset))
            {
                try { encoding = Encoding.GetEncoding(charset.Replace("\"", "")); } catch { }
            }

            if (encoding == null)
            {
                string utf8Str = Encoding.UTF8.GetString(byteResult);
                if (utf8Str.Contains('�'))
                {
                    try { encoding = Encoding.GetEncoding("GBK"); } catch { encoding = Encoding.UTF8; }
                }
                else
                {
                    encoding = Encoding.UTF8;
                }
            }

            return encoding.GetString(byteResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"请求失败: {ex.Message}");
            return "";
        }
    }

    private static string CleanHtml(string html)
    {
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<!--[\s\S]*?-->", "");
        html = Regex.Replace(html, @"<head[^>]*>[\s\S]*?</head>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<(\w+)(?:\s+[^>]*)?>", "<$1>");
        html = Regex.Replace(html, @"<(\w+)(?:\s+[^>]*)?>\s*</\1>", "");
        html = html.Replace("\r\n", "\n").Replace("\r", "\n");
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");
        return html.Trim();
    }
}
