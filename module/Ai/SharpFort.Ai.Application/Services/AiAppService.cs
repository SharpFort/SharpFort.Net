using Mapster;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using SharpFort.Ai.Application.Contracts.Dtos.AiApp;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class AiAppService(
    ISqlSugarRepository<AiApp, Guid> repository,
    IAiSkillToolService skillToolService,
    IAiSkillToolBindService skillToolBindService) : ApplicationService, IAiAppService
{
    public async Task<PagedResultDto<AiAppDto>> GetListAsync(AiAppGetListInput input)
    {
        RefAsync<int> total = 0;
        var query = repository._DbQueryable
            .Where(t => !t.IsDeleted)
            .WhereIF(!string.IsNullOrEmpty(input.Keyword), t => t.Name!.Contains(input.Keyword!));

        var items = await query.OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<AiAppDto>(total, items.Adapt<List<AiAppDto>>());
    }

    public async Task<List<AiAppDto>> GetAllListAsync()
    {
        var items = await repository._DbQueryable
            .Where(t => !t.IsDeleted)
            .OrderByDescending(x => x.CreationTime).ToListAsync();
        return items.Adapt<List<AiAppDto>>();
    }

    public async Task<AiAppDto> GetAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        var dto = entity.Adapt<AiAppDto>();
        var skills = await skillToolService.GetAllSkillsAsync();
        var tools = await skillToolService.GetAllToolsAsync();
        var boundIds = await skillToolBindService.GetBoundSkillToolIdsAsync(id.ToString());
        dto.Skills = skills.Select(t => new AiAppBindSkillToolDto
        {
            AiSkillToolId = t.Id,
            AiSkillToolName = t.Name,
            IsSelect = boundIds.Contains(t.Id)
        }).ToList();
        dto.Tools = tools.Select(t => new AiAppBindSkillToolDto
        {
            AiSkillToolId = t.Id,
            AiSkillToolName = t.Name,
            IsSelect = boundIds.Contains(t.Id)
        }).ToList();
        return dto;
    }

    public async Task<AiAppDto> CreateAsync(AiAppDto input)
    {
        var entity = input.Adapt<AiApp>();
        entity.IsDeleted = false;
        await repository.InsertAsync(entity);
        await BindSkillToolsAsync(entity.Id.ToString(), input);
        return entity.Adapt<AiAppDto>();
    }

    public async Task<AiAppDto> UpdateAsync(Guid id, AiAppDto input)
    {
        var entity = await repository.GetByIdAsync(id);
        input.Adapt(entity);
        await repository.UpdateAsync(entity);
        await BindSkillToolsAsync(id.ToString(), input);
        return entity.Adapt<AiAppDto>();
    }

    public async Task DeleteAsync(Guid id)
    {
        await repository.DeleteAsync(id);
    }

    public Task<AiAppDto> NewInitializationAsync()
    {
        var dto = new AiAppDto { Id = Guid.NewGuid() };
        var skills = skillToolService.GetAllSkillsAsync().Result;
        var tools = skillToolService.GetAllToolsAsync().Result;
        dto.Skills = skills.Select(t => new AiAppBindSkillToolDto
        {
            AiSkillToolId = t.Id,
            AiSkillToolName = t.Name,
            IsSelect = true
        }).ToList();
        dto.Tools = tools.Select(t => new AiAppBindSkillToolDto
        {
            AiSkillToolId = t.Id,
            AiSkillToolName = t.Name,
            IsSelect = true
        }).ToList();
        return Task.FromResult(dto);
    }

    private async Task BindSkillToolsAsync(string bindId, AiAppDto dto)
    {
        var ids = dto.Skills.Where(t => t.IsSelect).Select(t => t.AiSkillToolId).ToList();
        ids.AddRange(dto.Tools.Where(t => t.IsSelect).Select(t => t.AiSkillToolId));
        await skillToolBindService.BatchBindAsync(bindId, ids);
    }
}
