using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Dictionary;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// DictionaryData服务抽象
    /// </summary>
    public interface IDictionaryDataService : ISfCrudAppService<DictionaryGetOutputDto, DictionaryGetListOutputDto, Guid, DictionaryGetListInputVo, DictionaryCreateInputVo, DictionaryUpdateInputVo>
    {

    }
}
