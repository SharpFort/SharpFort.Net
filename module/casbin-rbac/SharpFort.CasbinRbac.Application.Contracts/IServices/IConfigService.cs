using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Config;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Config服务抽象
    /// </summary>
    public interface IConfigService : ISfCrudAppService<ConfigGetOutputDto, ConfigGetListOutputDto, Guid, ConfigGetListInputVo, ConfigCreateInputVo, ConfigUpdateInputVo>
    {

    }
}
