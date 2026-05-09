using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services;

public class TemplateService(ISqlSugarRepository<Template, Guid> repository) : SfCrudAppService<Template, TemplateDto, Guid, TemplateGetListInput>(repository), ITemplateService
{
    private readonly ISqlSugarRepository<Template, Guid> _repository = repository;

    public async override Task<PagedResultDto<TemplateDto>> GetListAsync([FromQuery] TemplateGetListInput input)
    {
        RefAsync<int> total = 0;
        List<Template> entities = await _repository._DbQueryable.WhereIF(input.Name is not null, x => x.Name == input.Name)
                  .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        return new PagedResultDto<TemplateDto>
        {
            TotalCount = total,
            Items = await MapToGetListOutputDtosAsync(entities)
        };
    }
}

