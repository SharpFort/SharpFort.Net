using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services;

/// <summary>
/// Scriban 代码生成模板 CRUD 服务
/// 管理代码生成模板库，支持查看、新增、编辑、删除 Scriban 模板内容及生成路径
/// </summary>
public class TemplateService(ISqlSugarRepository<Template, Guid> repository) : SfCrudAppService<Template, TemplateDto, Guid, TemplateGetListInput>(repository), ITemplateService
{
    private readonly ISqlSugarRepository<Template, Guid> _repository = repository;

    /// <summary>
    /// 分页查询模板列表，支持按模板名称模糊筛选
    /// </summary>
    public override async Task<PagedResultDto<TemplateDto>> GetListAsync([FromQuery] TemplateGetListInput input)
    {
        RefAsync<int> total = 0;
        List<Template> entities = await _repository._DbQueryable.WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
                  .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        return new PagedResultDto<TemplateDto>
        {
            TotalCount = total,
            Items = await MapToGetListOutputDtosAsync(entities)
        };
    }
}

