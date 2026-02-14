using Volo.Abp.Application.Services;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiPrompt;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// AI提示词服务接口
/// </summary>
public interface IAiPromptService : IApplicationService
{
    /// <summary>
    /// 获取提示词列表
    /// </summary>
    Task<PagedResultDto<AiPromptDto>> GetListAsync(AiPromptGetListInput input);

    /// <summary>
    /// 根据ID获取提示词
    /// </summary>
    Task<AiPromptDto> GetAsync(Guid id);

    /// <summary>
    /// 创建提示词
    /// </summary>
    Task<AiPromptDto> CreateAsync(AiPromptCreateInput input);

    /// <summary>
    /// 更新提示词
    /// </summary>
    Task<AiPromptDto> UpdateAsync(Guid id, AiPromptUpdateInput input);

    /// <summary>
    /// 删除提示词
    /// </summary>
    Task DeleteAsync(Guid id);
}
