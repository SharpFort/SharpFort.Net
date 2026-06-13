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
    /// <summary>
    /// 实体注册表 (YiTable) CRUD 服务
    /// 管理所有 C# Entity 类的元数据注册表，支持查询、新增、编辑、删除
    /// </summary>
    public class TableService(ISqlSugarRepository<Table, Guid> repository) : SfCrudAppService<Table, TableDto, Guid, TableGetListInput>(repository), ITableService
    {
        private readonly ISqlSugarRepository<Table, Guid> _repository = repository;

        /// <summary>
        /// 分页查询实体注册表列表，支持按实体名称模糊筛选
        /// </summary>
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
