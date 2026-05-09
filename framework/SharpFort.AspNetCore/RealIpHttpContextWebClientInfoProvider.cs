using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyCSharp.HttpUserAgentParser.Providers;
using Volo.Abp.AspNetCore.WebClientInfo;

namespace SharpFort.AspNetCore;

/// <summary>
/// 真实IP地址提供程序,支持代理服务器场景
/// </summary>
/// <remarks>
/// 初始化真实IP地址提供程序的新实例
/// </remarks>
public partial class RealIpHttpContextWebClientInfoProvider(
    ILogger<HttpContextWebClientInfoProvider> logger,
    IHttpContextAccessor httpContextAccessor,
    IHttpUserAgentParserProvider httpUserAgentParser) : HttpContextWebClientInfoProvider(logger, httpContextAccessor, httpUserAgentParser)
{
    private const string XForwardedForHeader = "X-Forwarded-For";

    private readonly ILogger _logger = logger;

    /// <summary>
    /// 获取客户端IP地址,优先从X-Forwarded-For头部获取
    /// </summary>
    /// <returns>客户端IP地址</returns>
    protected override string? GetClientIpAddress()
    {
        try
        {
            var httpContext = HttpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            var headers = httpContext.Request?.Headers;
            if (headers != null && headers.TryGetValue(XForwardedForHeader, out var forwardedValues))
            {
                var forwardedIp = forwardedValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedIp))
                {
                    httpContext.Connection.RemoteIpAddress = IPAddress.Parse(forwardedIp);
                }
            }

            return httpContext.Connection?.RemoteIpAddress?.ToString();
        }
        catch (Exception ex)
        {
            LogClientIpError(ex);
            return null;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "获取客户端IP地址时发生异常")]
    private partial void LogClientIpError(Exception ex);
}
