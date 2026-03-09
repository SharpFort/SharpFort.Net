using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Config;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services
{
    /// <summary>
    /// Config服务实现
    /// </summary>
    public class ConfigService : SfCrudAppService<Config, ConfigGetOutputDto, ConfigGetListOutputDto, Guid,
            ConfigGetListInputVo, ConfigCreateInputVo, ConfigUpdateInputVo>,
        IConfigService
    {
        private ISqlSugarRepository<Config, Guid> _repository;

        public ConfigService(ISqlSugarRepository<Config, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 新增配置
        /// </summary>
        /// <param name="input">配置创建信息</param>
        /// <returns>创建后的配置信息</returns>
        public override Task<ConfigGetOutputDto> CreateAsync(ConfigCreateInputVo input)
        {
            return base.CreateAsync(input);
        }

        /// <summary>
        /// 修改配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="input">配置更新信息</param>
        /// <returns>更新后的配置信息</returns>
        public override Task<ConfigGetOutputDto> UpdateAsync(Guid id, ConfigUpdateInputVo input)
        {
            return base.UpdateAsync(id, input);
        }

        /// <summary>
        /// 分页查询配置列表
        /// </summary>
        /// <param name="input">查询条件(支持按配置键、配置名称、时间范围筛选)</param>
        /// <returns>配置分页列表数据</returns>
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

        /// <summary>
        /// 获取单个配置详情
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>配置详情数据</returns>
        public override Task<ConfigGetOutputDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }

        /// <summary>
        /// 批量删除配置
        /// </summary>
        /// <param name="ids">配置ID集合</param>
        public override Task DeleteAsync(IEnumerable<Guid> ids)
        {
            return base.DeleteAsync(ids);
        }

        /// <summary>
        /// 获取配置动态下拉框列表
        /// </summary>
        /// <param name="keywords">查询关键字</param>
        /// <returns>配置下拉框数据列表</returns>
        public override Task<PagedResultDto<ConfigGetListOutputDto>> GetSelectDataListAsync(string? keywords = null)
        {
            return base.GetSelectDataListAsync(keywords);
        }

        /// <summary>
        /// 导出配置Excel
        /// </summary>
        /// <param name="input">查询条件</param>
        /// <returns>Excel文件流</returns>
        public override Task<Microsoft.AspNetCore.Mvc.IActionResult> GetExportExcelAsync(ConfigGetListInputVo input)
        {
            return base.GetExportExcelAsync(input);
        }

        /// <summary>
        /// 导入配置Excel
        /// </summary>
        /// <param name="input">配置列表数据</param>
        public override Task PostImportExcelAsync(List<ConfigCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
    }
}