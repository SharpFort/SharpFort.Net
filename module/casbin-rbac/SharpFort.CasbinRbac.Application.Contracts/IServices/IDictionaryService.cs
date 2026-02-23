using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Dictionary;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Dictionary服务抽象
    /// </summary>
    public interface IDictionaryService : ISfCrudAppService<DictionaryGetOutputDto, DictionaryGetListOutputDto, Guid, DictionaryGetListInputVo, DictionaryCreateInputVo, DictionaryUpdateInputVo>
    {

    }
}
