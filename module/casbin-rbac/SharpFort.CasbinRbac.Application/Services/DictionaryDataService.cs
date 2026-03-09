using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Dictionary;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;


namespace SharpFort.CasbinRbac.Application.Services
{
    /// <summary>
    /// Dictionary服务实现
    /// </summary>
    public class DictionaryDataService : SfCrudAppService<Dictionary, DictionaryGetOutputDto, DictionaryGetListOutputDto, Guid, DictionaryGetListInputVo, DictionaryCreateInputVo, DictionaryUpdateInputVo>,
       IDictionaryDataService
    {
        private ISqlSugarRepository<Dictionary, Guid> _repository;
        public DictionaryDataService(ISqlSugarRepository<Dictionary, Guid> repository) : base(repository)
        {
            _repository= repository;
        }

        /// <summary>
        /// 新增字典数据
        /// </summary>
        /// <param name="input">字典数据创建信息</param>
        /// <returns>创建后的字典数据</returns>
        public override Task<DictionaryGetOutputDto> CreateAsync(DictionaryCreateInputVo input)
        {
            return base.CreateAsync(input);
        }

        protected override async Task CheckCreateInputDtoAsync(DictionaryCreateInputVo input)
        {
            // 防重复校验：同一字典类型下，字典值必须唯一
            var isExist = await _repository.IsAnyAsync(x => x.DictType == input.DictType && x.DictValue == input.DictValue);
            if (isExist)
            {
                throw new UserFriendlyException($"当前字典类型下已存在值为 {input.DictValue} 的字典数据!");
            }
        }

        /// <summary>
        /// 更新字典数据
        /// </summary>
        /// <param name="id">字典数据ID</param>
        /// <param name="input">字典数据更新信息</param>
        /// <returns>更新后的数据</returns>
        public override Task<DictionaryGetOutputDto> UpdateAsync(Guid id, DictionaryUpdateInputVo input)
        {
            return base.UpdateAsync(id, input);
        }

        protected override async Task CheckUpdateInputDtoAsync(Dictionary entity, DictionaryUpdateInputVo input)
        {
            // 防键值冲突校验：修改时确保不会和已有的其他字典值冲突
            var isExist = await _repository._DbQueryable.Where(x => x.Id != entity.Id)
                .AnyAsync(x => x.DictType == input.DictType && x.DictValue == input.DictValue);
            if (isExist)
            {
                throw new UserFriendlyException($"当前字典类型下已存在冲突的值为 {input.DictValue} 的字典数据!");
            }
        }

        /// <summary>
        /// 查询
        /// </summary>

        public override async Task<PagedResultDto<DictionaryGetListOutputDto>> GetListAsync(DictionaryGetListInputVo input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable
                .WhereIF(input.DictType is not null, x => x.DictType == input.DictType)
                .WhereIF(input.DictLabel is not null, x => x.DictLabel!.Contains(input.DictLabel!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .OrderByDescending(x => x.OrderNum)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
            return new PagedResultDto<DictionaryGetListOutputDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }


        /// <summary>
        /// 根据字典类型获取字典列表
        /// </summary>
        /// <param name="dicType"></param>
        /// <returns></returns>
        [Route("type/{dicType}")]
        public async Task<List<DictionaryGetListOutputDto>> GetDicType([FromRoute] string dicType)
        {
            var entities = await _repository.GetListAsync(u => u.DictType == dicType && u.State == true);
            var result = await MapToGetListOutputDtosAsync(entities);
            return result;
        }

        /// <summary>
        /// 获取单个字典数据详情
        /// </summary>
        /// <param name="id">字典数据ID</param>
        /// <returns>字典数据详情</returns>
        public override Task<DictionaryGetOutputDto> GetAsync(Guid id)
        {
            return base.GetAsync(id);
        }

        /// <summary>
        /// 批量删除字典数据
        /// </summary>
        /// <param name="ids">字典数据ID集合</param>
        public override Task DeleteAsync(IEnumerable<Guid> ids)
        {
            return base.DeleteAsync(ids);
        }

        /// <summary>
        /// 导出字典数据Excel
        /// </summary>
        /// <param name="input">查询条件</param>
        /// <returns>Excel文件流</returns>
        public override Task<Microsoft.AspNetCore.Mvc.IActionResult> GetExportExcelAsync(DictionaryGetListInputVo input)
        {
            return base.GetExportExcelAsync(input);
        }

        /// <summary>
        /// 导入字典数据Excel
        /// </summary>
        /// <param name="input">字典数据列表</param>
        public override Task PostImportExcelAsync(List<DictionaryCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
        
        /// <summary>
        /// 获取实体动态下拉框列表
        /// </summary>
        /// <param name="keywords">查询关键字</param>
        /// <returns>动态下拉框列表</returns>
        public override Task<PagedResultDto<DictionaryGetListOutputDto>> GetSelectDataListAsync(string? keywords = null)
        {
            return base.GetSelectDataListAsync(keywords);
        }
    }
}
