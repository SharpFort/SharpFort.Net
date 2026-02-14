using Volo.Abp.Application.Dtos;
using Yi.Framework.Ai.Application.Contracts.Dtos.Model;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// 模型服务接口
/// </summary>
public interface IModelService
{
    /// <summary>
    /// 获取模型库列表（公开接口，无需登录）
    /// </summary>
    /// <param name="input">查询参数</param>
    /// <returns>分页模型列表</returns>
    Task<PagedResultDto<ModelLibraryDto>> GetListAsync(ModelLibraryGetListInput input);

    /// <summary>
    /// 获取供应商列表（公开接口，无需登录）
    /// </summary>
    /// <returns>供应商列表</returns>
    Task<List<string>> GetProviderListAsync();

    /// <summary>
    /// 获取模型类型选项列表（公开接口，无需登录）
    /// </summary>
    /// <returns>模型类型选项</returns>
    Task<List<ModelTypeOption>> GetModelTypeOptionsAsync();

    /// <summary>
    /// 获取API类型选项列表（公开接口，无需登录）
    /// </summary>
    /// <returns>API类型选项</returns>
    Task<List<ModelApiTypeOption>> GetApiTypeOptionsAsync();
}
