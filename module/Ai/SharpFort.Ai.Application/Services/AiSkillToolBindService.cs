using SqlSugar;
using Volo.Abp.Application.Services;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class AiSkillToolBindService(
    ISqlSugarRepository<AiSkillToolBind, Guid> repository) : ApplicationService, IAiSkillToolBindService
{
    public async Task<List<Guid>> GetBoundSkillToolIdsAsync(string bindId)
    {
        var items = await repository._DbQueryable
            .Where(t => !t.IsDeleted && t.BindId == bindId)
            .Select(t => t.AiSkillToolId)
            .ToListAsync();
        return items;
    }

    public async Task BatchBindAsync(string bindId, List<Guid> skillToolIds)
    {
        var existing = await repository._DbQueryable
            .Where(t => !t.IsDeleted && t.BindId == bindId)
            .ToListAsync();

        foreach (var item in existing)
        {
            if (!skillToolIds.Contains(item.AiSkillToolId))
                await repository.DeleteAsync(item);
            else
                skillToolIds.Remove(item.AiSkillToolId);
        }

        foreach (var id in skillToolIds)
        {
            var bind = new AiSkillToolBind
            {
                BindId = bindId,
                AiSkillToolId = id,
                IsDeleted = false
            };
            await repository.InsertAsync(bind);
        }
    }
}
