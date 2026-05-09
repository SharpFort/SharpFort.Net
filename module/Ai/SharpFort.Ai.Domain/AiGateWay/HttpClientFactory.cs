using System.Collections.Concurrent;

namespace SharpFort.Ai.Domain.AiGateWay;

public static class HttpClientFactory
{
    private static int PoolSize
    {
        get
        {
            if (field == 0)
            {
                // 获取环境变量
                string? poolSize = Environment.GetEnvironmentVariable("HttpClientPoolSize");
                field = !string.IsNullOrEmpty(poolSize) && int.TryParse(poolSize, out int size) ? size : Environment.ProcessorCount;

                if (field < 1)
                {
                    field = 2;
                }
            }

            return field;
        }
    }

    private static readonly ConcurrentDictionary<string, Lazy<List<HttpClient>>> HttpClientPool = new();

    /// <summary>
    /// 高并发下有问题
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [Obsolete("Use IHttpClientFactory.CreateClient instead")]
    public static HttpClient GetHttpClient(string key)
    {
        return HttpClientPool.GetOrAdd(key, k => new Lazy<List<HttpClient>>(() =>
        {
            List<HttpClient> clients = new(PoolSize);

            for (int i = 0; i < PoolSize; i++)
            {
                clients.Add(new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(30),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(30),
                    EnableMultipleHttp2Connections = true,
                    // 连接超时5分钟
                    ConnectTimeout = TimeSpan.FromMinutes(5),
                    MaxAutomaticRedirections = 3,
                    AllowAutoRedirect = true,
                    Expect100ContinueTimeout = TimeSpan.FromMinutes(30),
                })
                {
                    Timeout = TimeSpan.FromMinutes(30),
                    DefaultRequestHeaders =
                    {
                        { "User-Agent", "yxai" },
                    }
                });
            }

            return clients;
        })).Value[new Random().Next(0, PoolSize)];
    }
}
