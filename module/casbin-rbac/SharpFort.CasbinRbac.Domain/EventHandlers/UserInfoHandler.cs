using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Shared.Etos;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;

namespace SharpFort.CasbinRbac.Domain.EventHandlers
{
    public class UserInfoHandler(UserManager userManager) : ILocalEventHandler<UserRoleMenuQueryEventArgs>, ITransientDependency
    {
        private readonly UserManager _userManager = userManager;

        public async Task HandleEventAsync(UserRoleMenuQueryEventArgs eventData)
        {
            //数据库查询方式
            List<UserRoleMenuDto> result = await _userManager.GetInfoListAsync(eventData.UserIds);
            eventData.Result = result;
        }
    }
}
