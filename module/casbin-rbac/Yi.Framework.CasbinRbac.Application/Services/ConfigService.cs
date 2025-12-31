using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Config;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Application.Services
{
    /// <summary>
    /// Config服务实现
    /// </summary>
    public class ConfigService : YiCrudAppService<Config, ConfigGetOutputDto, ConfigGetListOutputDto, Guid,
            ConfigGetListInputVo, ConfigCreateInputVo, ConfigUpdateInputVo>,
        IConfigService
    {
        private ISqlSugarRepository<Config, Guid> _repository;

        public ConfigService(ISqlSugarRepository<Config, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 多查
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<PagedResultDto<ConfigGetListOutputDto>> GetListAsync(ConfigGetListInputVo input)
        {
            RefAsync<int> total = 0;

            var entities = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.ConfigKey),
                    x => x.ConfigKey.Contains(input.ConfigKey!))
                .WhereIF(!string.IsNullOrEmpty(input.ConfigName), x => x.ConfigName!.Contains(input.ConfigName!))
                .WhereIF(input.StartTime is not null && input.EndTime is not null,
                    x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<ConfigGetListOutputDto>(total, await MapToGetListOutputDtosAsync(entities));
        }

        protected override async Task CheckCreateInputDtoAsync(ConfigCreateInputVo input)
        {
            var isExist =
                await _repository.IsAnyAsync(x => x.ConfigKey == input.ConfigKey);
            if (isExist)
            {
                throw new UserFriendlyException(ConfigConst.Exist);
            }
        }

        protected override async Task CheckUpdateInputDtoAsync(Config entity, ConfigUpdateInputVo input)
        {
            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.ConfigKey == input.ConfigKey);
            if (isExist)
            {
                throw new UserFriendlyException(ConfigConst.Exist);
            }
        }
    }
}