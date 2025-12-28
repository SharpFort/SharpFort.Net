using IPTools.Core;
using Microsoft.AspNetCore.Http;
using System;
using UAParser;
using Yi.Framework.Core.Extensions;


// 引用 IpTool 的命名空间
namespace Yi.Framework.Core.Helper
{
    public static class ClientInfoHelper
    {
        public class ClientResult
        {
            public string LoginIp { get; set; }
            public string LoginLocation { get; set; }
            public string Browser { get; set; }
            public string Os { get; set; }
        }

        public static ClientResult GetClientInfo(HttpContext context)
        {
            if (context == null) return new ClientResult();

            // 1. 解析 UserAgent (浏览器和OS)
            var uaStr = context.GetUserAgent(); // 假设这是你的扩展方法
            var uaParser = Parser.GetDefault();
            ClientInfo c;
            try
            {
                c = uaParser.Parse(uaStr);
            }
            catch
            {
                // 降级处理
                c = new ClientInfo("null", new OS("null", "null", "null", "null", "null"), new Device("null", "null", "null"), new UserAgent("null", "null", "null", "null"));
            }
            //return c;

            string browser = c?.Device?.Family ?? "Unknown";
            string os = c?.OS?.ToString() ?? "Unknown";

            // 2. 解析 IP 和 地理位置
            var ipAddr = context.GetClientIp(); // 假设这是你的扩展方法
            string locationStr;

            if (ipAddr == "127.0.0.1" || ipAddr == "::1")
            {
                locationStr = "本地-本机";
            }
            else
            {
                try
                {
                    var location = IpTool.Search(ipAddr); // 假设 IpTool 是静态工具
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