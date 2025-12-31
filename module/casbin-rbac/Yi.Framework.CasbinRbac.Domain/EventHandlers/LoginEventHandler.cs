using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapster;
using Volo.Abp.Guids; // 引用 Guid 生成器命名空间
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Etos;

namespace Yi.Framework.CasbinRbac.Domain.EventHandlers
{
    public class LoginEventHandler : ILocalEventHandler<LoginEventArgs>,
          ITransientDependency
    {
        private readonly ILogger<LoginEventHandler> _logger;
        private readonly IRepository<LoginLog> _loginLogRepository;
        private readonly IGuidGenerator _guidGenerator; // 1. 注入 Guid 生成器
        public LoginEventHandler(
            ILogger<LoginEventHandler> logger, 
            IRepository<LoginLog> loginLogRepository,
             IGuidGenerator guidGenerator) { _logger = logger; _loginLogRepository = loginLogRepository; }
        public async Task HandleEventAsync(LoginEventArgs eventData)
        {
            _logger.LogInformation($"用户【{eventData.UserId}:{eventData.UserName}】登入系统");
            //var loginLogEntity = eventData.Adapt<LoginLog>();
            //loginLogEntity.LogMsg = eventData.UserName + "登录系统";
            //loginLogEntity.LoginUser = eventData.UserName;
            // 2. 使用构造函数创建实体
            // 不再使用 eventData.Adapt<LoginLog>(); 
            // 显式创建更清晰，且解决了 protected set 的问题
            var loginLogEntity = new LoginLog(
                id: _guidGenerator.Create(),
                loginUser: eventData.UserName,
                logMsg: eventData.UserName + "登录系统",
                loginIp: eventData.LoginIp, // <--- 传入 IP
                loginLocation: eventData.LoginLocation, // 传入登录地点
                browser: eventData.Browser, // 如果 Event 里有就传，没有就传 null
                os: eventData.Os,           // 同上
                creatorId: eventData.UserId
            );
            loginLogEntity.CreatorId = eventData.UserId;
            //异步插入
            await _loginLogRepository.InsertAsync(loginLogEntity);
        }
    }
}
