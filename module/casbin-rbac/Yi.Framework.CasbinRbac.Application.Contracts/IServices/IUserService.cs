using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.User;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// User服务抽象
    /// </summary>
    public interface IUserService : IYiCrudAppService<UserGetOutputDto, UserGetListOutputDto, Guid, UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>
    {
    }
}
