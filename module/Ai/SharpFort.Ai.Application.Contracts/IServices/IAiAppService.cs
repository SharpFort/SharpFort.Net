using SharpFort.Ai.Application.Contracts.Dtos.AiApp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SharpFort.Ai.Application.Contracts.IServices;

public interface IAiAppService : IApplicationService
{
    Task<PagedResultDto<AiAppDto>> GetListAsync(AiAppGetListInput input);
    Task<List<AiAppDto>> GetAllListAsync();
    Task<AiAppDto> GetAsync(Guid id);
    Task<AiAppDto> CreateAsync(AiAppDto input);
    Task<AiAppDto> UpdateAsync(Guid id, AiAppDto input);
    Task DeleteAsync(Guid id);
    Task<AiAppDto> NewInitializationAsync();
}
