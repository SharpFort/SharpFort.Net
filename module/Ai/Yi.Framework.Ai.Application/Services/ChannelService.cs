using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.Channel;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// 渠道商管理服务实现
/// </summary>
[Authorize(Roles = "admin")]
public class ChannelService : ApplicationService, IChannelService
{
    private readonly ISqlSugarRepository<AiProvider, Guid> _appRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;
    private readonly ISqlSugarRepository<AiAppShortcutAggregateRoot, Guid> _appShortcutRepository;

    public ChannelService(
        ISqlSugarRepository<AiProvider, Guid> appRepository,
        ISqlSugarRepository<AiModel, Guid> modelRepository,
        ISqlSugarRepository<AiAppShortcutAggregateRoot, Guid> appShortcutRepository)
    {
        _appRepository = appRepository;
        _modelRepository = modelRepository;
        _appShortcutRepository = appShortcutRepository;
    }

    #region AI应用管理

    /// <summary>
    /// 获取AI应用列表
    /// </summary>
    [HttpGet("channel/app")]
    public async Task<PagedResultDto<AiAppDto>> GetAppListAsync(AiAppGetListInput input)
    {
        RefAsync<int> total = 0;

        var entities = await _appRepository._DbQueryable
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), x => x.Name.Contains(input.SearchKey))
            .OrderByDescending(x => x.OrderNum)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        var output = entities.Adapt<List<AiAppDto>>();
        return new PagedResultDto<AiAppDto>(total, output);
    }

    /// <summary>
    /// 根据ID获取AI应用
    /// </summary>
    [HttpGet("channel/app/{id}")]
    public async Task<AiAppDto> GetAppByIdAsync([FromRoute]Guid id)
    {
        var entity = await _appRepository.GetByIdAsync(id);
        return entity.Adapt<AiAppDto>();
    }

    /// <summary>
    /// 创建AI应用
    /// </summary>
    public async Task<AiAppDto> CreateAppAsync(AiAppCreateInput input)
    {
        var entity = new AiProvider
        {
            Name = input.Name,
            Endpoint = input.Endpoint,
            ExtraUrl = input.ExtraUrl,
            ApiKey = input.ApiKey,
            OrderNum = input.OrderNum
        };

        await _appRepository.InsertAsync(entity);
        return entity.Adapt<AiAppDto>();
    }

    /// <summary>
    /// 更新AI应用
    /// </summary>
    public async Task<AiAppDto> UpdateAppAsync(AiAppUpdateInput input)
    {
        var entity = await _appRepository.GetByIdAsync(input.Id);

        entity.Name = input.Name;
        entity.Endpoint = input.Endpoint;
        entity.ExtraUrl = input.ExtraUrl;
        entity.ApiKey = input.ApiKey;
        entity.OrderNum = input.OrderNum;

        await _appRepository.UpdateAsync(entity);
        return entity.Adapt<AiAppDto>();
    }

    /// <summary>
    /// 删除AI应用
    /// </summary>
    [HttpDelete("channel/app/{id}")]
    public async Task DeleteAppAsync([FromRoute]Guid id)
    {
        // 检查是否有关联的模型
        var hasModels = await _modelRepository._DbQueryable
            .Where(x => x.AiProviderId == id && !x.IsDeleted)
            .AnyAsync();

        if (hasModels)
        {
            throw new Volo.Abp.UserFriendlyException("该应用下存在模型,无法删除");
        }

        await _appRepository.DeleteAsync(id);
    }

    #endregion

    #region AI模型管理

    /// <summary>
    /// 获取AI模型列表
    /// </summary>
    [HttpGet("channel/model")]
    public async Task<PagedResultDto<AiModelDto>> GetModelListAsync(AiModelGetListInput input)
    {
        RefAsync<int> total = 0;

        var query = _modelRepository._DbQueryable
            .Where(x => !x.IsDeleted)
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), x =>
                x.Name.Contains(input.SearchKey) || x.ModelId.Contains(input.SearchKey))
            .WhereIF(input.AiAppId.HasValue, x => x.AiProviderId == input.AiAppId.Value);


        var entities = await query
            .OrderBy(x => x.OrderNum)
            .OrderByDescending(x => x.Id)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        var output = entities.Adapt<List<AiModelDto>>();
        return new PagedResultDto<AiModelDto>(total, output);
    }

    /// <summary>
    /// 根据ID获取AI模型
    /// </summary>
    [HttpGet("channel/model/{id}")]
    public async Task<AiModelDto> GetModelByIdAsync([FromRoute]Guid id)
    {
        var entity = await _modelRepository.GetByIdAsync(id);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 创建AI模型
    /// </summary>
    public async Task<AiModelDto> CreateModelAsync(AiModelCreateInput input)
    {
        // 验证应用是否存在
        var appExists = await _appRepository._DbQueryable
            .Where(x => x.Id == input.AiAppId)
            .AnyAsync();

        if (!appExists)
        {
            throw new Volo.Abp.UserFriendlyException("指定的AI应用不存在");
        }

        var entity = new AiModel
        {
            HandlerName = input.HandlerName,
            ModelId = input.ModelId,
            Name = input.Name,
            Description = input.Description,
            OrderNum = input.OrderNum,
            AiProviderId = input.AiAppId,
            ExtraInfo = input.ExtraInfo,
            ModelType = input.ModelType,
            ModelApiType = input.ModelApiType,
            Multiplier = input.Multiplier,
            MultiplierShow = input.MultiplierShow,
            ProviderName = input.ProviderName,
            IconUrl = input.IconUrl,
            IsEnabled = input.IsEnabled,
            IsDeleted = false
        };

        await _modelRepository.InsertAsync(entity);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 更新AI模型
    /// </summary>
    public async Task<AiModelDto> UpdateModelAsync(AiModelUpdateInput input)
    {
        var entity = await _modelRepository.GetByIdAsync(input.Id);

        // 验证应用是否存在
        if (entity.AiProviderId != input.AiAppId)
        {
            var appExists = await _appRepository._DbQueryable
                .Where(x => x.Id == input.AiAppId)
                .AnyAsync();

            if (!appExists)
            {
                throw new Volo.Abp.UserFriendlyException("指定的AI应用不存在");
            }
        }

        entity.HandlerName = input.HandlerName;
        entity.ModelId = input.ModelId;
        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.OrderNum = input.OrderNum;
        entity.AiProviderId = input.AiAppId;
        entity.ExtraInfo = input.ExtraInfo;
        entity.ModelType = input.ModelType;
        entity.ModelApiType = input.ModelApiType;
        entity.Multiplier = input.Multiplier;
        entity.MultiplierShow = input.MultiplierShow;
        entity.ProviderName = input.ProviderName;
        entity.IconUrl = input.IconUrl;
        entity.IsEnabled = input.IsEnabled;
        await _modelRepository.UpdateAsync(entity);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 删除AI模型(软删除)
    /// </summary>
    [HttpDelete("channel/model/{id}")]
    public async Task DeleteModelAsync(Guid id)
    {
        await _modelRepository.DeleteByIdAsync(id);
    }

    #endregion

    #region AI应用快捷配置

    /// <summary>
    /// 获取AI应用快捷配置列表
    /// </summary>
    [HttpGet("channel/app-shortcut")]
    public async Task<List<AiAppShortcutDto>> GetAppShortcutListAsync()
    {
        var entities = await _appShortcutRepository._DbQueryable
            .OrderBy(x => x.OrderNum)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync();

        return entities.Adapt<List<AiAppShortcutDto>>();
    }

    #endregion
}
