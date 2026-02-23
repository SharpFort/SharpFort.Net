using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.DictionaryType;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// DictionaryType服务抽象
    /// </summary>
    public interface IDictionaryTypeService : ISfCrudAppService<DictionaryTypeGetOutputDto, DictionaryTypeGetListOutputDto, Guid, DictionaryTypeGetListInputVo, DictionaryTypeCreateInputVo, DictionaryTypeUpdateInputVo>
    {

    }
}
