using SharpFort.CodeGen.Application.Contracts.Dtos.Table;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    public interface ITableService : ISfCrudAppService<TableDto, Guid, TableGetListInput>
    {
    }
}
