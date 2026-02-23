using SharpFort.CodeGen.Application.Contracts.Dtos.Field;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    public interface IFieldService : ISfCrudAppService<FieldDto, Guid, FieldGetListInput>
    {
    }
}
