using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Menu服务抽象
    /// </summary>
    public interface IMenuService : ISfCrudAppService<MenuGetOutputDto, MenuGetListOutputDto, Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>
    {

    }
}
