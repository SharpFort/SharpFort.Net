using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using SharpFort.CodeGen.Application.Contracts.Dtos.Field;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// 实体字段 (SfField) 服务
    /// 管理实体注册表下的字段元数据，数据由反射扫描自动填充 (Code→Web)，用户仅需调整 UI 配置参数
    /// </summary>
    public class FieldService : SfCrudAppService<Field, FieldDto, Guid, FieldGetListInput>, IFieldService
    {
        private readonly ISqlSugarRepository<Field, Guid> _repository;
        private readonly ISqlSugarRepository<Table> _tableRepository;

        public FieldService(
            ISqlSugarRepository<Field, Guid> repository,
            ISqlSugarRepository<Table> tableRepository) : base(repository)
        {
            _repository = repository;
            _tableRepository = tableRepository;
        }

        /// <summary>
        /// 分页查询字段列表：获取指定实体下的所有字段定义，支持按字段名称模糊筛选
        /// 场景：查看某实体的字段结构及 UI 配置（查询条件/列表显示/表单字段/控件类型）
        /// </summary>
        /// <param name="input">查询参数：TableId (必填，指定所属实体)、Name (可选，字段名称模糊筛选)</param>
        public override async Task<PagedResultDto<FieldDto>> GetListAsync([FromQuery] FieldGetListInput input)
        {
            RefAsync<int> total = 0;
            List<Field> entities = await _repository._DbQueryable
                      .WhereIF(input.TableId is not null, x => x.TableId == input.TableId)
                      .WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
                      .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            var items = await MapToGetListOutputDtosAsync(entities);

            // 批量填充 TableName / ModuleName（避免在 Select 中 new Entity 导致 CS0272）
            await FillTableInfoAsync(items);

            return new PagedResultDto<FieldDto>
            {
                TotalCount = total,
                Items = items
            };
        }

        /// <summary>
        /// 获取字段详情：查看单个字段的完整信息
        /// 包含结构属性（字段名/类型/长度/是否主键/是否必填）和 UI 配置（查询/列表/表单/控件类型/排序）
        /// </summary>
        /// <param name="id">字段 ID</param>
        public override async Task<FieldDto> GetAsync(Guid id)
        {
            await CheckGetPolicyAsync();

            Field entity = await _repository._DbQueryable
                .Where(x => x.Id == id)
                .FirstAsync() ?? throw new EntityNotFoundException(typeof(Field), id);

            var dto = await MapToGetOutputDtoAsync(entity);

            // 填充 TableName / ModuleName
            var table = await _tableRepository._DbQueryable
                .Where(x => x.Id == entity.TableId)
                .Select(x => new { x.Name, x.ModuleName })
                .FirstAsync();
            if (table != null)
            {
                dto.TableName = table.Name;
                dto.ModuleName = table.ModuleName;
            }

            return dto;
        }

        /// <summary>
        /// 更新字段 UI 配置：修改字段在代码生成时的行为参数
        /// 可配置项：IsQueryField (是否生成查询条件)、IsListDisplay (是否在列表 DTO 中生成)、
        /// IsFormItem (是否在表单 DTO 中生成)、HtmlType (前端控件类型: Input/Select/DatePicker/Textarea/Switch)、
        /// OrderNum (字段排序权重)、Description (字段备注)
        /// 注意：字段名/类型/长度等结构性属性由反射同步维护，修改后下次同步可能被覆盖
        /// </summary>
        /// <param name="id">字段 ID</param>
        /// <param name="input">更新输入 DTO</param>
        public override async Task<FieldDto> UpdateAsync(Guid id, FieldDto input)
        {
            await CheckUpdatePolicyAsync();

            Field entity = await GetEntityByIdAsync(id);
            await CheckUpdateInputDtoAsync(entity, input);
            await MapToEntityAsync(input, entity);
            await Repository.UpdateAsync(entity, autoSave: true);

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 批量填充多个 FieldDto 的 TableName 和 ModuleName
        /// </summary>
        private async Task FillTableInfoAsync(List<FieldDto> items)
        {
            if (items.Count == 0) return;

            var tableIds = items.Select(x => x.TableId).Distinct().ToList();
            var tables = await _tableRepository._DbQueryable
                .Where(x => tableIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name, x.ModuleName })
                .ToListAsync();
            var tableMap = tables.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                if (tableMap.TryGetValue(item.TableId, out var t))
                {
                    item.TableName = t.Name;
                    item.ModuleName = t.ModuleName;
                }
            }
        }

        /// <summary>
        /// 获取字段类型枚举列表：返回所有可用的 FieldType 枚举值
        /// 枚举值：String / Int / Long / Bool / Decimal / DateTime / Guid / Float / Double
        /// 用途：前端字段编辑表单中 FieldType 下拉框的数据源
        /// </summary>
        /// <returns>字段类型枚举列表，包含 label (显示名) 和 value (整数值)</returns>
        [Route("field/type")]
#pragma warning disable CA1822 // ABP requires instance methods for AutoAPI
        public object GetFieldType()
        {
            return typeof(FieldType).GetFields(BindingFlags.Static | BindingFlags.Public).Select(x => new { label = x.Name, value = (int)Enum.Parse<FieldType>(x.Name) }).ToList();
        }
#pragma warning restore CA1822

        #region 禁用端点 — Field 数据由反射 (Code→Web) 自动填充，以下端点不适用

        /// <summary>
        /// [已禁用] 手动创建字段无意义 — Field 由 PostRefreshAsync 反射扫描 C# 属性自动填充，下次同步会清除手动创建的记录
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task<FieldDto> CreateAsync(FieldDto input) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 批量删除字段无持久效果 — 同步时采用全量删除重建策略，手动删除的字段会在下次 PostRefreshAsync 后恢复
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task DeleteAsync(IEnumerable<Guid> ids) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 字段始终在 Table 上下文中通过 TableId 查询，无独立下拉引用场景
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task<PagedResultDto<FieldDto>> GetSelectDataListAsync(string? keywords = null) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 字段元数据是系统数据，不适合 Excel 导出
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task<IActionResult> GetExportExcelAsync(FieldGetListInput input) => throw new NotImplementedException();

        /// <summary>
        /// [已禁用] 字段由反射填充，Excel 导入无意义且基类未实现
        /// </summary>
        [RemoteService(isEnabled: false)]
        public override Task PostImportExcelAsync(List<FieldDto> input) => throw new NotImplementedException();

        #endregion
    }
}
