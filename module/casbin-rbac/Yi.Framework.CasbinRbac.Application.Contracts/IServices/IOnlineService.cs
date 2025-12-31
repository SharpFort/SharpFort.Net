using Volo.Abp.Application.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Model;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices
{
    public interface IOnlineService
    {
      Task< PagedResultDto<OnlineUserModel>> GetListAsync(OnlineUserModel online);
    }
}
