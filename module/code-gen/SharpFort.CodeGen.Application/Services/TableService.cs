using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Table;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    public class TableService(ISqlSugarRepository<Table, Guid> repository) : SfCrudAppService<Table, TableDto, Guid, TableGetListInput>(repository), ITableService
    {
        private readonly ISqlSugarRepository<Table, Guid> _repository = repository;

        public override async Task<PagedResultDto<TableDto>> GetListAsync([FromQuery] TableGetListInput input)
        {
            RefAsync<int> total = 0;
            List<Table> entities = await _repository._DbQueryable
                .WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            return new PagedResultDto<TableDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }
    }
}
