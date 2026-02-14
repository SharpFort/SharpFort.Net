using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.Channel;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// 渠道商管理服务接口
/// </summary>
public interface IChannelService
{
    #region AI应用管理

    /// <summary>
    /// 获取AI应用列表
    /// </summary>
    /// <param name="input">查询参数</param>
    /// <returns>分页应用列表</returns>
    Task<PagedResultDto<AiAppDto>> GetAppListAsync(AiAppGetListInput input);

    /// <summary>
    /// 根据ID获取AI应用
    /// </summary>
    /// <param name="id">应用ID</param>
    /// <returns>应用详情</returns>
    Task<AiAppDto> GetAppByIdAsync(Guid id);

    /// <summary>
    /// 创建AI应用
    /// </summary>
    /// <param name="input">创建输入</param>
    /// <returns>创建的应用</returns>
    Task<AiAppDto> CreateAppAsync(AiAppCreateInput input);

    /// <summary>
    /// 更新AI应用
    /// </summary>
    /// <param name="input">更新输入</param>
    /// <returns>更新后的应用</returns>
    Task<AiAppDto> UpdateAppAsync(AiAppUpdateInput input);

    /// <summary>
    /// 删除AI应用
    /// </summary>
    /// <param name="id">应用ID</param>
    Task DeleteAppAsync(Guid id);

    #endregion

    #region AI模型管理

    /// <summary>
    /// 获取AI模型列表
    /// </summary>
    /// <param name="input">查询参数</param>
    /// <returns>分页模型列表</returns>
    Task<PagedResultDto<AiModelDto>> GetModelListAsync(AiModelGetListInput input);

    /// <summary>
    /// 根据ID获取AI模型
    /// </summary>
    /// <param name="id">模型ID</param>
    /// <returns>模型详情</returns>
    Task<AiModelDto> GetModelByIdAsync(Guid id);

    /// <summary>
    /// 创建AI模型
    /// </summary>
    /// <param name="input">创建输入</param>
    /// <returns>创建的模型</returns>
    Task<AiModelDto> CreateModelAsync(AiModelCreateInput input);

    /// <summary>
    /// 更新AI模型
    /// </summary>
    /// <param name="input">更新输入</param>
    /// <returns>更新后的模型</returns>
    Task<AiModelDto> UpdateModelAsync(AiModelUpdateInput input);

    /// <summary>
    /// 删除AI模型(软删除)
    /// </summary>
    /// <param name="id">模型ID</param>
    Task DeleteModelAsync(Guid id);

    #endregion

    #region AI应用快捷配置

    /// <summary>
    /// 获取AI应用快捷配置列表
    /// </summary>
    /// <returns>快捷配置列表</returns>
    Task<List<AiAppShortcutDto>> GetAppShortcutListAsync();

    #endregion
}
