using Mapster;
using Volo.Abp.Guids;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Etos;

namespace SharpFort.CasbinRbac.Domain.EventHandlers
{
    public class LoginEventHandler : ILocalEventHandler<LoginEventArgs>,
          ITransientDependency
    {
        private readonly ILogger<LoginEventHandler> _logger;
        private readonly IRepository<LoginLog> _loginLogRepository;
        private readonly IGuidGenerator _guidGenerator;

        public LoginEventHandler(
            ILogger<LoginEventHandler> logger,
            IRepository<LoginLog> loginLogRepository,
            IGuidGenerator guidGenerator)
        {
            _logger = logger;
            _loginLogRepository = loginLogRepository;
            _guidGenerator = guidGenerator; // 修复 Bug: 之前构造函数未赋值，导致 _guidGenerator 永远是 null
        }

        public async Task HandleEventAsync(LoginEventArgs eventData)
        {
            _logger.LogInformation("用户【{UserId}:{UserName}】登入系统", eventData.UserId, eventData.UserName);

            var loginLogEntity = new LoginLog(
                id: _guidGenerator.Create(),
                loginUser: eventData.UserName,
                logMsg: eventData.UserName + "登录系统",
                loginIp: eventData.LoginIp ?? string.Empty, // CS8604: LoginIp 可能为 null，使用空字符串替代
                loginLocation: eventData.LoginLocation,
                browser: eventData.Browser,
                os: eventData.Os,
                creatorId: eventData.UserId
            );
            loginLogEntity.CreatorId = eventData.UserId;
            await _loginLogRepository.InsertAsync(loginLogEntity);
        }
    }
}
