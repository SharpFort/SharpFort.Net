using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.OperLog;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Operlog;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.RecordLog
{
    /// <summary>
    /// OperationLog服务实现
    /// </summary>
    public class OperationLogService(ISqlSugarRepository<OperationLogEntity, Guid> repository) : SfCrudAppService<OperationLogEntity, OperationLogGetListOutputDto, Guid, OperationLogGetListInputVo>(repository),
       IOperationLogService
    {
        private readonly ISqlSugarRepository<OperationLogEntity, Guid> _repository = repository;

        public override async Task<PagedResultDto<OperationLogGetListOutputDto>> GetListAsync(OperationLogGetListInputVo input)
        {
            RefAsync<int> total = 0;
            //if (input.Sorting.IsNullOrWhiteSpace())
            //    input.Sorting = $"{nameof(OperationLogEntity.CreationTime)} Desc";
            List<OperationLogEntity> entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.OperUser), x => x.OperUser!.Contains(input.OperUser!))
                          .WhereIF(input.OperationType is not null, x => x.OperationType == input.OperationType)
                          .WhereIF(input.StartTime is not null && input.EndTime is not null, x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)
                          .OrderByDescending(it => it.CreationTime) //降序
                          .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<OperationLogGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        [RemoteService(false)]
        public override Task<OperationLogGetListOutputDto> UpdateAsync(Guid id, OperationLogGetListOutputDto input)
        {
            return base.UpdateAsync(id, input);
        }
    }
}
