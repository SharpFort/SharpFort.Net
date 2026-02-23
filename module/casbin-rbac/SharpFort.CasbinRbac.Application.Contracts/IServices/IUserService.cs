using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.User;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// User服务抽象
    /// </summary>
    public interface IUserService : ISfCrudAppService<UserGetOutputDto, UserGetListOutputDto, Guid, UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>
    {
    }
}
