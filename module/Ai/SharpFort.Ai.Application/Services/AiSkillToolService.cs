using Mapster;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using SharpFort.Ai.Application.Contracts.Dtos.AiSkillTool;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class AiSkillToolService(
    ISqlSugarRepository<AiSkillTool, Guid> repository) : ApplicationService, IAiSkillToolService
{
    public async Task<PagedResultDto<AiSkillToolDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        RefAsync<int> total = 0;
        var items = await repository._DbQueryable
            .Where(t => !t.IsDeleted)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<AiSkillToolDto>(total, items.Adapt<List<AiSkillToolDto>>());
    }

    public async Task<List<AiSkillToolDto>> GetAllSkillsAsync()
    {
        var items = await repository._DbQueryable
            .Where(t => !t.IsDeleted && t.SkillToolType == AiSkillToolType.Skill && t.IsEnabled)
            .ToListAsync();
        return items.Adapt<List<AiSkillToolDto>>();
    }

    public async Task<List<AiSkillToolDto>> GetAllToolsAsync()
    {
        var items = await repository._DbQueryable
            .Where(t => !t.IsDeleted && t.SkillToolType == AiSkillToolType.Tool && t.IsEnabled)
            .ToListAsync();
        return items.Adapt<List<AiSkillToolDto>>();
    }

    public async Task<AiSkillToolDto> CreateAsync(AiSkillToolDto input)
    {
        var entity = input.Adapt<AiSkillTool>();
        entity.IsSystem = false;
        entity.IsDeleted = false;
        await repository.InsertAsync(entity);
        return entity.Adapt<AiSkillToolDto>();
    }

    public async Task<AiSkillToolDto> UpdateAsync(Guid id, AiSkillToolDto input)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity.IsSystem)
            throw new UserFriendlyException("系统内置工具不允许修改");
        input.Adapt(entity);
        entity.IsSystem = false;
        await repository.UpdateAsync(entity);
        return entity.Adapt<AiSkillToolDto>();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity.IsSystem)
            throw new UserFriendlyException("系统内置工具不允许删除");
        await repository.DeleteAsync(id);
    }
}
