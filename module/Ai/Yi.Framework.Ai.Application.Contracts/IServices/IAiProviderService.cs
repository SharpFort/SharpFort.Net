using Volo.Abp.Application.Services;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiProvider;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// AI供应商管理服务接口
/// </summary>
public interface IAiProviderService : IApplicationService
{
    /// <summary>
    /// 获取AI供应商列表
    /// </summary>
    Task<PagedResultDto<AiProviderDto>> GetListAsync(AiProviderGetListInput input);

    /// <summary>
    /// 根据ID获取AI供应商
    /// </summary>
    Task<AiProviderDto> GetAsync(Guid id);

    /// <summary>
    /// 创建AI供应商
    /// </summary>
    Task<AiProviderDto> CreateAsync(AiProviderCreateInput input);

    /// <summary>
    /// 更新AI供应商
    /// </summary>
    Task<AiProviderDto> UpdateAsync(Guid id, AiProviderUpdateInput input);

    /// <summary>
    /// 删除AI供应商
    /// </summary>
    Task DeleteAsync(Guid id);
}
