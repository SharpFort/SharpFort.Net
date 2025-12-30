using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// HTTP 请求辅助类（使用静态 HttpClient 实例）
    /// </summary>
    /// <remarks>
    /// Socket 耗尽问题说明：
    ///
    /// ✅ 本类使用静态 HttpClient 实例，这是正确的做法：
    /// - HttpClient 设计为实例化一次并重复使用
    /// - 每次请求创建新实例会导致 Socket 耗尽（TIME_WAIT 状态堆积）
    /// - 静态实例自动管理连接池
    ///
    /// 使用场景：
    /// - 简单的 HTTP 请求（不需要复杂配置）
    /// - 控制台应用、后台任务
    /// - 不需要依赖注入的场景
    ///
    /// 何时使用 IHttpClientFactory（推荐）：
    /// <code>
    /// // 在 ASP.NET Core 中注册
    /// services.AddHttpClient("MyClient", client =&gt;
    /// {
    ///     client.Timeout = TimeSpan.FromSeconds(30);
    ///     client.DefaultRequestHeaders.Add("User-Agent", "MyApp");
    /// });
    ///
    /// // 在服务中使用
    /// public class MyService
    /// {
    ///     private readonly HttpClient _client;
    ///     public MyService(IHttpClientFactory factory)
    ///     {
    ///         _client = factory.CreateClient("MyClient");
    ///     }
    /// }
    /// </code>
    ///
    /// IHttpClientFactory 优势：
    /// - 自动轮换处理程序（避免 DNS 陈旧问题）
    /// - 可配置重试策略（Polly）
    /// - 内置日志记录
    /// - 依赖注入友好
    ///
    /// 本类的局限性：
    /// - 默认超时 100 秒（可能不适合所有场景）
    /// - 无重试机制
    /// - DNS 变更不会自动刷新（长期运行的应用可能受影响）
    /// - 无日志记录
    /// </remarks>
    public static class HttpHelper
    {
        /// <summary>
        /// 静态 HttpClient 实例（避免 Socket 耗尽）
        /// </summary>
        /// <remarks>
        /// ⚠️ 注意事项：
        /// - 不要频繁替换此实例
        /// - 如需配置超时，在应用启动时设置：HttpHelper.Client.Timeout = TimeSpan.FromSeconds(30);
        /// - 长期运行的应用考虑使用 IHttpClientFactory 以处理 DNS 刷新
        /// </remarks>
        public static HttpClient Client { get; private set; } = new HttpClient();

        /// <summary>
        /// 发送 GET 请求获取字符串响应
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <returns>响应内容字符串</returns>
        /// <exception cref="HttpRequestException">请求失败时抛出</exception>
        /// <remarks>
        /// 使用场景：
        /// - 获取 API 响应（JSON/XML）
        /// - 下载文本内容
        ///
        /// 示例：
        /// <code>
        /// var json = await HttpHelper.Get("https://api.example.com/data");
        /// var data = JsonSerializer.Deserialize&lt;MyDto&gt;(json);
        /// </code>
        /// </remarks>
        public static async Task<string> Get(string url)
        {
            return await Client.GetStringAsync(url);
        }

        /// <summary>
        /// 发送 GET 请求获取流响应
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <returns>响应内容流</returns>
        /// <exception cref="HttpRequestException">请求失败时抛出</exception>
        /// <remarks>
        /// 使用场景：
        /// - 下载文件（图片、PDF 等）
        /// - 处理大数据响应（避免全部加载到内存）
        ///
        /// 示例：
        /// <code>
        /// using var stream = await HttpHelper.GetIO("https://example.com/image.png");
        /// using var fileStream = File.Create("local.png");
        /// await stream.CopyToAsync(fileStream);
        /// </code>
        ///
        /// ⚠️ 注意：调用方负责释放返回的 Stream
        /// </remarks>
        public static async Task<Stream> GetIO(string url)
        {
            return await Client.GetStreamAsync(url);
        }

        /// <summary>
        /// 发送 POST 请求（JSON 格式）
        /// </summary>
        /// <param name="url">请求 URL</param>
        /// <param name="item">要序列化为 JSON 的对象（可选）</param>
        /// <param name="head">自定义请求头（可选）</param>
        /// <returns>响应内容字符串</returns>
        /// <exception cref="HttpRequestException">请求失败或响应状态码非 2xx 时抛出</exception>
        /// <remarks>
        /// 使用场景：
        /// - 调用 REST API
        /// - 提交表单数据（JSON 格式）
        ///
        /// 示例：
        /// <code>
        /// var result = await HttpHelper.Post(
        ///     "https://api.example.com/users",
        ///     new { Name = "张三", Age = 30 },
        ///     new Dictionary&lt;string, string&gt; { ["Authorization"] = "Bearer token" }
        /// );
        /// </code>
        ///
        /// 注意：
        /// - 使用 System.Text.Json 序列化
        /// - 自动设置 Content-Type: application/json
        /// - 调用 EnsureSuccessStatusCode() 检查响应状态
        /// </remarks>
        public static async Task<string> Post(string url, object? item = null, Dictionary<string, string>? head = null)
        {

            using StringContent json = new(JsonSerializer.Serialize(item), Encoding.UTF8, MediaTypeNames.Application.Json);


            if (head is not null)
            {
                foreach (var d in head)
                {
                    json.Headers.Add(d.Key, d.Value);
                }
            }

            var httpResponse = await Client.PostAsync(url, json);

            httpResponse.EnsureSuccessStatusCode();

            var content = httpResponse.Content;

            return await content.ReadAsStringAsync();
        }
    }
}
