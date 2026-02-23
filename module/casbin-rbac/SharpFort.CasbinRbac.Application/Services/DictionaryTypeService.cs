using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.DictionaryType;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services
{
    /// <summary>
    /// DictionaryType服务实现
    /// </summary>
    public class DictionaryTypeService : SfCrudAppService<DictionaryType, DictionaryTypeGetOutputDto, DictionaryTypeGetListOutputDto, Guid, DictionaryTypeGetListInputVo, DictionaryTypeCreateInputVo, DictionaryTypeUpdateInputVo>,
       IDictionaryTypeService
    {
        private ISqlSugarRepository<DictionaryType, Guid> _repository;
        public DictionaryTypeService(ISqlSugarRepository<DictionaryType, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        public override async Task<PagedResultDto<DictionaryTypeGetListOutputDto>> GetListAsync(DictionaryTypeGetListInputVo input)
        {

            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable.WhereIF(input.DictName is not null, x => x.DictName.Contains(input.DictName!))
                      .WhereIF(input.DictType is not null, x => x.DictType!.Contains(input.DictType!))
                      .WhereIF(input.State is not null, x => x.State == input.State)
                      .WhereIF(input.StartTime is not null && input.EndTime is not null, x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)
                      .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            return new PagedResultDto<DictionaryTypeGetListOutputDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }

        protected override async Task CheckCreateInputDtoAsync(DictionaryTypeCreateInputVo input)
        {
            var isExist =
                await _repository.IsAnyAsync(x => x.DictType == input.DictType);
            if (isExist)
            {
                throw new UserFriendlyException(DictionaryConst.Exist);
            }
        }

        protected override async Task CheckUpdateInputDtoAsync(DictionaryType entity, DictionaryTypeUpdateInputVo input)
        {
            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.DictType == input.DictType);
            if (isExist)
            {
                throw new UserFriendlyException(DictionaryConst.Exist);
            }
        }
    }
}
