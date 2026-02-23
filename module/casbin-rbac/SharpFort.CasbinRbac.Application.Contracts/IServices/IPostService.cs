using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Post服务抽象
    /// </summary>
    public interface IPostService : ISfCrudAppService<PostGetOutputDto, PostGetListOutputDto, Guid, PostGetListInputVo, PostCreateInputVo, PostUpdateInputVo>
    {

    }
}
