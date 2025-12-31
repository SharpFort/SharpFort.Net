using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Dictionary;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Dictionary服务抽象
    /// </summary>
    public interface IDictionaryService : IYiCrudAppService<DictionaryGetOutputDto, DictionaryGetListOutputDto, Guid, DictionaryGetListInputVo, DictionaryCreateInputVo, DictionaryUpdateInputVo>
    {

    }
}
