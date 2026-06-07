using IPTools.Core;
using Microsoft.AspNetCore.Http;
using UAParser;
using SharpFort.Core.Extensions;


// 引用 IpTool 的命名空间
namespace SharpFort.Core.Helper
{
    public static class ClientInfoHelper
    {
        // 单例：应用启动时初始化一次，避免高并发下重复解析 regexes.yaml
        private static readonly Parser _uaParser = Parser.GetDefault();

        public class ClientResult
        {
            public string LoginIp { get; set; } = string.Empty;
            public string LoginLocation { get; set; } = string.Empty;
            public string Browser { get; set; } = string.Empty;
            public string Os { get; set; } = string.Empty;
        }

        public static ClientResult GetClientInfo(HttpContext context)
        {
            if (context == null)
            {
                return new ClientResult();
            }

            // 1. 解析 UserAgent (浏览器和OS)
            string uaStr = context.GetUserAgent(); // 假设这是你的扩展方法
            ClientInfo c;
            try
            {
                c = _uaParser.Parse(uaStr);
            }
            catch
            {
                // 降级处理
                c = new ClientInfo("null", new OS("null", "null", "null", "null", "null"), new Device("null", "null", "null"), new UserAgent("null", "null", "null", "null"));
            }
            //return c;

            string browser = c?.UA?.Family ?? "Unknown";
            string os = c?.OS?.ToString() ?? "Unknown";

            // 2. 解析 IP 和 地理位置
            string ipAddr = context.GetClientIp(); // 假设这是你的扩展方法
            string locationStr;

            if (ipAddr is "127.0.0.1" or "::1")
            {
                locationStr = "本地-本机";
            }
            else
            {
                try
                {
                    IpInfo location = IpTool.Search(ipAddr); // 假设 IpTool 是静态工具
                    locationStr = $"{location.Province}-{location.City}";
                }
                catch
                {
                    locationStr = "未知地区";
                }
            }

            return new ClientResult
            {
                LoginIp = ipAddr,
                LoginLocation = locationStr,
                Browser = browser,
                Os = os
            };
        }
    }
}