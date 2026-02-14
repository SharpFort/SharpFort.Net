using Volo.Abp.Application.Services;
using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiModel;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// AI模型管理服务接口
/// </summary>
public interface IAiModelService : IApplicationService
{
    /// <summary>
    /// 获取AI模型列表
    /// </summary>
    Task<PagedResultDto<AiModelDto>> GetListAsync(AiModelGetListInput input);

    /// <summary>
    /// 根据ID获取AI模型
    /// </summary>
    Task<AiModelDto> GetAsync(Guid id);

    /// <summary>
    /// 创建AI模型
    /// </summary>
    Task<AiModelDto> CreateAsync(AiModelCreateInput input);

    /// <summary>
    /// 更新AI模型
    /// </summary>
    Task<AiModelDto> UpdateAsync(Guid id, AiModelUpdateInput input);

    /// <summary>
    /// 删除AI模型
    /// </summary>
    Task DeleteAsync(Guid id);
}
