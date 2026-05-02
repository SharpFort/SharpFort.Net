using System;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using SharpFort.CodeGen.Application.Contracts.Dtos.Field;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// 字段管理
    /// </summary>
    public class FieldService : SfCrudAppService<Field, FieldDto, Guid, FieldGetListInput>, IFieldService
    {
        private ISqlSugarRepository<Field, Guid> _repository;
        public FieldService(ISqlSugarRepository<Field, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        public async override Task<PagedResultDto<FieldDto>> GetListAsync([FromQuery] FieldGetListInput input)
        {
            RefAsync<int> total = 0;
            var entities = await _repository._DbQueryable.WhereIF(input.TableId is not null, x => x.TableId == input.TableId)
                      .WhereIF(input.Name is not null, x => x.Name.Contains(input.Name!))

                      .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            return new PagedResultDto<FieldDto>
            {
                TotalCount = total,
                Items = await MapToGetListOutputDtosAsync(entities)
            };
        }

        /// <summary>
        /// 获取类型枚举
        /// </summary>
        /// <returns></returns>
        [Route("field/type")]
#pragma warning disable CA1822 // ABP requires instance methods for AutoAPI
        public object GetFieldType()
        {
            return typeof(FieldType).GetFields(BindingFlags.Static | BindingFlags.Public).Select(x => new { lable = x.Name, value = (int)Enum.Parse<FieldType>(x.Name) }).ToList();
        }
#pragma warning restore CA1822
    }
}
