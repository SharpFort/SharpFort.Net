using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using SharpFort.CodeGen.Application.Contracts.Dtos.Table;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;

namespace SharpFort.CodeGen.Application.Services
{
    public class TableService(IRepository<Table, Guid> repository) : SfCrudAppService<Table, TableDto, Guid, TableGetListInput>(repository), ITableService
    {
        public override Task<PagedResultDto<TableDto>> GetListAsync(TableGetListInput input)
        {
            return base.GetListAsync(input);
        }
    }
}
