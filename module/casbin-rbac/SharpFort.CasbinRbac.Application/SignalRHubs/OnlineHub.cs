using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using Volo.Abp.AspNetCore.SignalR;
using SharpFort.Core.Helper;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Model;

namespace SharpFort.CasbinRbac.Application.SignalRHubs
{
    [HubRoute("/hub/main")]
    //开放不需要授权
    //[Authorize]
    public partial class OnlineHub : AbpHub
    {
        public static ConcurrentDictionary<string, OnlineUserModel> ClientUsersDic { get; set; } = new();

        private readonly HttpContext? _httpContext;
        private readonly ILogger<OnlineHub> _logger;

        public OnlineHub(IHttpContextAccessor httpContextAccessor, ILogger<OnlineHub> logger)
        {
            _httpContext = httpContextAccessor?.HttpContext;
            _logger = logger;
        }


        /// <summary>
        /// 成功连接
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync()
        {
            if (_httpContext is null)
            {
                return Task.CompletedTask;
            }
            var name = CurrentUser.UserName;
            var loginUser = ClientInfoHelper.GetClientInfo(_httpContext);
            //var loginUser = ClientInfoHelper.GetClientInfo(httpContext);

            OnlineUserModel user = new(Context.ConnectionId)
            {
                Browser = loginUser?.Browser,
                LoginLocation = loginUser?.LoginLocation,
                Ipaddr = loginUser?.LoginIp,
                LoginTime = DateTime.Now,
                Os = loginUser?.Os,
                UserName = name ?? "Null",
                UserId = CurrentUser.Id ?? Guid.Empty
            };

            //已登录
            if (CurrentUser.IsAuthenticated)
            {
                ClientUsersDic.RemoveAll(u => u.Value.UserId == CurrentUser.Id);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    LogUserConnected(timestamp, name ?? "Unknown", Context.ConnectionId, ClientUsersDic.Count);
                }
            }

            ClientUsersDic.AddOrUpdate(Context.ConnectionId, user, (_, _) => user);

            //当有人加入，向全部客户端发送当前总数
            Clients.All.SendAsync("onlineNum", ClientUsersDic.Count);

            return base.OnConnectedAsync();
        }


        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //已登录
            if (CurrentUser.IsAuthenticated)
            {
                ClientUsersDic.RemoveAll(u => u.Value.UserId == CurrentUser.Id);
                LogUserDisconnected(CurrentUser?.UserName ?? "Unknown", ClientUsersDic.Count);
            }
            ClientUsersDic.Remove(Context.ConnectionId, out _);
            Clients.All.SendAsync("onlineNum", ClientUsersDic.Count);
            return base.OnDisconnectedAsync(exception);
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{Time}：{UserName},{ConnectionId}连接服务端success，当前已连接{Count}个")]
        private partial void LogUserConnected(string time, string userName, string connectionId, int count);

        [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "用户{UserName}离开了，当前已连接{Count}个")]
        private partial void LogUserDisconnected(string userName, int count);
    }
}