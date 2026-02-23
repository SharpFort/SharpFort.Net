using Mapster;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Repositories;
using SharpFort.CasbinRbac.Domain.Shared.Caches;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;
using SharpFort.CasbinRbac.Domain.Shared.Etos;

namespace SharpFort.CasbinRbac.Domain.EventHandlers
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
