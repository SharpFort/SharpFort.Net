using SharpFort.Ai.Application.Contracts.Dtos.AiKms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SharpFort.Ai.Application.Contracts.IServices;

public interface IAiKmsService : IApplicationService
{
    Task<PagedResultDto<AiKmsDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<List<AiKmsDto>> GetAllListAsync();
    Task<AiKmsDto> GetAsync(Guid id);
    Task<AiKmsDto> CreateAsync(AiKmsDto input);
    Task<AiKmsDto> UpdateAsync(Guid id, AiKmsDto input);
    Task DeleteAsync(Guid id);
    Task ProcessKmssVectorDataAsync(Guid? kmsId = null);
}
