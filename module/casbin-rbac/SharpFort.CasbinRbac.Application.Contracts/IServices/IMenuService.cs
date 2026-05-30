using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Menu服务抽象
    /// </summary>
    public interface IMenuService : ISfCrudAppService<MenuGetOutputDto, MenuGetListOutputDto, Guid, MenuGetListInputVo, MenuCreateInputVo, MenuUpdateInputVo>
    {
        /// <summary>
        /// 本地高速缓存预热（尽力而为，失败不阻断启动）
        /// </summary>
        Task WarmupCacheAsync();
    }
}
