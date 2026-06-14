using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using SharpFort.CodeGen.Application.Contracts.Dtos.Table;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// 实体注册表 (YiTable) 服务
    /// 管理所有 C# Entity 类的元数据注册表，数据由反射扫描自动填充 (Code→Web)，用户仅需配置生成参数
    /// </summary>
    public class TableService(ISqlSugarRepository<Table, Guid> repository) : SfCrudAppService<Table, TableDto, Guid, TableGetListInput>(repository), ITableService
    {
        private readonly ISqlSugarRepository<Table, Guid> _repository = repository;

        /// <summary>
        /// 分页查询实体注册表列表：列出所有通过反射扫描注册的 C# Entity 类
        /// 支持按实体名称模糊筛选、按所属模块/项目精确筛选（值来自搜索栏下拉框）
        /// 场景：代码生成页面的实体列表数据源，用户在此选择目标实体后执行代码生成
        /// </summary>
        public override async Task<PagedResultDto<TableDto>> GetListAsync([FromQuery] TableGetListInput input)
        {
            RefAsync<int> total = 0;
            List<Table> entities = await _repository._DbQueryable
                .WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
                .WhereIF(input.ModuleName is not null, x => x.ModuleName == input.ModuleName)
                .WhereIF(input.ProjectName is not null, x => x.ProjectName == input.ProjectName)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            return new PagedResultDto<TableDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }

        /// <summary>
        /// 获取实体注册表详情：查看单个实体的完整配置信息，同时包含关联的字段列表
        /// 场景：实体详情页面一次请求获取 Table 配置 + 全部 Field 列表，减少前端请求次数
        /// </summary>
        /// <param name="id">实体注册表 ID</param>
        public override async Task<TableDto> GetAsync(Guid id)
        {
            await CheckGetPolicyAsync();

            Table entity = await _repository._DbQueryable
                .Includes(x => x.Fields)
                .Where(x => x.Id == id)
                .FirstAsync() ?? throw new EntityNotFoundException(typeof(Table), id);

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 更新实体注册表配置：修改实体的代码生成参数（所属模块、命名空间、是否覆盖等）
        /// 注意：实体类名 (Name) 和物理表名 (PhysicalTableName) 由反射同步维护，不建议手动修改
        /// </summary>
        /// <param name="id">实体注册表 ID</param>
        /// <param name="input">更新输入 DTO</param>
        public override async Task<TableDto> UpdateAsync(Guid id, TableDto input)
        {
            await CheckUpdatePolicyAsync();

            Table entity = await GetEntityByIdAsync(id);
            await CheckUpdateInputDtoAsync(entity, input);
            await MapToEntityAsync(input, entity);
            await Repository.UpdateAsync(entity, autoSave: true);

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 获取搜索栏下拉框数据：返回所有实体注册表条目的 Id/名称/模块/项目信息
        /// 支持通过 keywords 对名称、模块、项目进行模糊过滤
        /// 前端使用方式：按 ModuleName/ProjectName 分组去重，填充搜索栏下拉框
        /// </summary>
        /// <param name="keywords">过滤关键字（可选），匹配实体名/模块名/项目名</param>
        public override async Task<PagedResultDto<TableDto>> GetSelectDataListAsync(string? keywords = null)
        {
            ISugarQueryable<Table> query = _repository._DbQueryable;
            if (!string.IsNullOrEmpty(keywords))
            {
                query = query.Where(x => x.Name!.Contains(keywords)
                    || (x.ModuleName != null && x.ModuleName.Contains(keywords))
                    || (x.ProjectName != null && x.ProjectName.Contains(keywords)));
            }

            List<TableDto> items = await query
                .Select(x => new TableDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    ModuleName = x.ModuleName,
                    ProjectName = x.ProjectName
                })
                .ToListAsync();

            return new PagedResultDto<TableDto>(items.Count, items);
        }

        #region 禁用端点 — Table 数据由反射 (Code→Web) 自动填充，以下端点不适用

        /// <summary>
        /// [已禁用] 手动创建实体注册表条目无意义 — Table 由 PostRefreshAsync 反射扫描自动填充
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task<TableDto> CreateAsync(TableDto input) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 批量删除会级联丢失所有 Field UI 配置 — 下次同步会重建但配置不可恢复
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task DeleteAsync(IEnumerable<Guid> ids) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 实体注册表是系统元数据，不适合 Excel 导出
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task<IActionResult> GetExportExcelAsync(TableGetListInput input) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 实体注册表由反射填充，Excel 导入无意义且基类未实现
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task PostImportExcelAsync(List<TableDto> input) => throw new NotImplementedException();

        #endregion
    }
}
