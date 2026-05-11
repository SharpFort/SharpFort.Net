using Volo.Abp.Guids;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Etos;

namespace SharpFort.CasbinRbac.Domain.EventHandlers
{
    public partial class LoginEventHandler(
        ILogger<LoginEventHandler> logger,
        IRepository<LoginLog> loginLogRepository,
        IGuidGenerator guidGenerator) : ILocalEventHandler<LoginEventArgs>,
          ITransientDependency
    {
        private readonly ILogger<LoginEventHandler> _logger = logger;
        private readonly IRepository<LoginLog> _loginLogRepository = loginLogRepository;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

        public async Task HandleEventAsync(LoginEventArgs eventData)
        {
            LogUserLogin(eventData.UserId, eventData.UserName);

            LoginLog loginLogEntity = new(
                id: _guidGenerator.Create(),
                loginUser: eventData.UserName,
                logMsg: eventData.UserName + "登录系统",
                loginIp: eventData.LoginIp ?? string.Empty,
                loginLocation: eventData.LoginLocation,
                browser: eventData.Browser,
                os: eventData.Os,
                creatorId: eventData.UserId
            )
            {
                CreatorId = eventData.UserId
            };
            await _loginLogRepository.InsertAsync(loginLogEntity);
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "用户【{UserId}:{UserName}】登入系统")]
        private partial void LogUserLogin(Guid userId, string userName);
    }
}
