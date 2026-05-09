using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Application.SignalRHubs;
using SharpFort.CasbinRbac.Domain.Shared.Model;
using System.Collections.Concurrent;

namespace SharpFort.CasbinRbac.Application.Services.Monitor
{
    public class OnlineService(ILogger<OnlineService> logger, IHubContext<OnlineHub> hub) : ApplicationService, IOnlineService
    {
        private readonly ILogger<OnlineService> _logger = logger;
        private readonly IHubContext<OnlineHub> _hub = hub;

        /// <summary>
        /// 动态条件获取当前在线用户
        /// </summary>
        /// <param name="online"></param>
        /// <returns></returns>
        public Task<PagedResultDto<OnlineUserModel>> GetListAsync([FromQuery] OnlineUserModel online)
        {
            ConcurrentDictionary<string, OnlineUserModel> data = OnlineHub.ClientUsersDic;
            IEnumerable<OnlineUserModel> dataWhere = data.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(online.Ipaddr))
            {
                dataWhere = dataWhere.Where((u) => u.Ipaddr!.Contains(online.Ipaddr));
            }

            if (!string.IsNullOrEmpty(online.UserName))
            {
                dataWhere = dataWhere.Where((u) => u.UserName!.Contains(online.UserName));
            }

            return Task.FromResult(new PagedResultDto<OnlineUserModel>()
            { TotalCount = data.Count, Items = dataWhere.ToList() });
        }


        /// <summary>
        /// 强制退出用户
        /// </summary>
        /// <param name="connnectionId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("online/{connnectionId}")]
        public async Task<bool> ForceOut(string connnectionId)
        {
            if (OnlineHub.ClientUsersDic.ContainsKey(connnectionId))
            {
                //前端接受到这个事件后，触发前端自动退出
                await _hub.Clients.Client(connnectionId).SendAsync("forceOut", "你已被强制退出！");
                return true;
            }

            return false;
        }
    }
}