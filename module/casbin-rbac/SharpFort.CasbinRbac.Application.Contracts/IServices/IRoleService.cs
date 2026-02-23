using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Role;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Role服务抽象
    /// </summary>
    public interface IRoleService : ISfCrudAppService<RoleGetOutputDto, RoleGetListOutputDto, Guid, RoleGetListInputVo, RoleCreateInputVo, RoleUpdateInputVo>
    {

    }
}
