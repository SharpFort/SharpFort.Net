using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Field;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// 实体字段 (YiField) CRUD 服务
    /// 管理实体注册表下的字段元数据，支持查询、新增、编辑、删除、类型枚举查询
    /// </summary>
    public class FieldService(ISqlSugarRepository<Field, Guid> repository) : SfCrudAppService<Field, FieldDto, Guid, FieldGetListInput>(repository), IFieldService
    {
        private readonly ISqlSugarRepository<Field, Guid> _repository = repository;

        /// <summary>
        /// 分页查询字段列表，支持按所属实体 ID 和字段名称筛选
        /// </summary>
        public override async Task<PagedResultDto<FieldDto>> GetListAsync([FromQuery] FieldGetListInput input)
        {
            RefAsync<int> total = 0;
            List<Field> entities = await _repository._DbQueryable.WhereIF(input.TableId is not null, x => x.TableId == input.TableId)
                      .WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))

                      .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            return new PagedResultDto<FieldDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }

        /// <summary>
        /// 获取字段类型枚举列表：返回所有可用的 FieldType 枚举值 (String/Int/Long/Bool/Decimal/DateTime/Guid/Float/Double)
        /// </summary>
        /// <returns>字段类型枚举列表，包含 label (显示名) 和 value (整数值)</returns>
        [Route("field/type")]
#pragma warning disable CA1822 // ABP requires instance methods for AutoAPI
        public object GetFieldType()
        {
            return typeof(FieldType).GetFields(BindingFlags.Static | BindingFlags.Public).Select(x => new { label = x.Name, value = (int)Enum.Parse<FieldType>(x.Name) }).ToList();
        }
#pragma warning restore CA1822
    }
}
