using Mapster;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Yi.Framework.CasbinRbac.Domain.Managers;
using Yi.Framework.CasbinRbac.Domain.Repositories;
using Yi.Framework.CasbinRbac.Domain.Shared.Caches;
using Yi.Framework.CasbinRbac.Domain.Shared.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Etos;

namespace Yi.Framework.CasbinRbac.Domain.EventHandlers
{
    public class UserInfoHandler : ILocalEventHandler<UserRoleMenuQueryEventArgs>, ITransientDependency
    {
        private UserManager _userManager;
        public UserInfoHandler(UserManager userManager)
        {
            _userManager = userManager;
        }
        public async Task HandleEventAsync(UserRoleMenuQueryEventArgs eventData)
        {
            //数据库查询方式
            var result = await _userManager.GetInfoListAsync(eventData.UserIds);
            eventData.Result = result;
        }
    }
}
