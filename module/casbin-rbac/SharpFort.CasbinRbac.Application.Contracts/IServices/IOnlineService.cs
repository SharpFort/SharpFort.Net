using Volo.Abp.Application.Dtos;
using SharpFort.CasbinRbac.Domain.Shared.Model;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    public interface IOnlineService
    {
      Task< PagedResultDto<OnlineUserModel>> GetListAsync(OnlineUserModel online);
    }
}
