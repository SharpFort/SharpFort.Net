using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services;

public class TemplateService : SfCrudAppService<Template, TemplateDto, Guid, TemplateGetListInput>, ITemplateService
{
    private ISqlSugarRepository<Template, Guid> _repository;
    public TemplateService(ISqlSugarRepository<Template, Guid> repository) : base(repository)
    {
        _repository = repository;
    }

    public async override Task<PagedResultDto<TemplateDto>> GetListAsync([FromQuery] TemplateGetListInput input)
    {
        RefAsync<int> total = 0;
        var entities = await _repository._DbQueryable.WhereIF(input.Name is not null, x => x.Name.Equals(input.Name!))
                  .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        return new PagedResultDto<TemplateDto>
        {
            TotalCount = total,
            Items = await MapToGetListOutputDtosAsync(entities)
        };
    }
}

