using Volo.Abp.Application.Services;

namespace SharpFort.Ai.Application.Contracts.IServices;

public interface IAiSkillToolBindService : IApplicationService
{
    Task<List<Guid>> GetBoundSkillToolIdsAsync(string bindId);
    Task BatchBindAsync(string bindId, List<Guid> skillToolIds);
}
