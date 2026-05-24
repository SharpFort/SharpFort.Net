using SharpFort.Ai.Application.Contracts.Dtos.AiSkillTool;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SharpFort.Ai.Application.Contracts.IServices;

public interface IAiSkillToolService : IApplicationService
{
    Task<PagedResultDto<AiSkillToolDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<List<AiSkillToolDto>> GetAllSkillsAsync();
    Task<List<AiSkillToolDto>> GetAllToolsAsync();
    Task<AiSkillToolDto> CreateAsync(AiSkillToolDto input);
    Task<AiSkillToolDto> UpdateAsync(Guid id, AiSkillToolDto input);
    Task DeleteAsync(Guid id);
}
