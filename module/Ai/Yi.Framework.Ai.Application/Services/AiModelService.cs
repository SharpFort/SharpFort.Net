using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiModel;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// AI模型管理服务
/// </summary>
[Authorize(Roles = "admin")]
public class AiModelService : ApplicationService, IAiModelService
{
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;
    private readonly ISqlSugarRepository<AiProvider, Guid> _providerRepository;

    public AiModelService(
        ISqlSugarRepository<AiModel, Guid> modelRepository,
        ISqlSugarRepository<AiProvider, Guid> providerRepository)
    {
        _modelRepository = modelRepository;
        _providerRepository = providerRepository;
    }

    /// <summary>
    /// 获取AI模型列表
    /// </summary>
    [HttpGet("ai-model")]
    public async Task<PagedResultDto<AiModelDto>> GetListAsync(AiModelGetListInput input)
    {
        RefAsync<int> total = 0;

        var query = _modelRepository._DbQueryable
            .Where(x => !x.IsDeleted)
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), x =>
                x.Name.Contains(input.SearchKey) || x.ModelId.Contains(input.SearchKey))
            .WhereIF(input.AiProviderId.HasValue, x => x.AiProviderId == input.AiProviderId.Value);
            // .WhereIF(input.IsPremiumOnly == true, x => x.IsPremium); // Assuming IsPremiumOnly is not in DTO or not needed yet

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
    [HttpGet("ai-model/{id}")]
    public async Task<AiModelDto> GetAsync([FromRoute] Guid id)
    {
        var entity = await _modelRepository.GetByIdAsync(id);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 创建AI模型
    /// </summary>
    [HttpPost("ai-model")]
    public async Task<AiModelDto> CreateAsync(AiModelCreateInput input)
    {
        // 验证供应商是否存在
        var providerExists = await _providerRepository._DbQueryable
            .Where(x => x.Id == input.AiProviderId)
            .AnyAsync();

        if (!providerExists)
        {
            throw new UserFriendlyException("指定的AI供应商不存在");
        }

        var entity = new AiModel
        {
            HandlerName = input.HandlerName,
            ModelId = input.ModelId,
            Name = input.Name,
            Description = input.Description,
            OrderNum = input.OrderNum,
            AiProviderId = input.AiProviderId,
            ExtraInfo = input.ExtraInfo,
            ModelType = input.ModelType,
            ModelApiType = input.ModelApiType,
            Multiplier = input.Multiplier,
            MultiplierShow = input.MultiplierShow,
            ProviderName = input.ProviderName,
            IconUrl = input.IconUrl,
            IsPremium = input.IsPremium, 
            IsEnabled = input.IsEnabled,
            IsDeleted = false
        };

        await _modelRepository.InsertAsync(entity);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 更新AI模型
    /// </summary>
    [HttpPut("ai-model/{id}")]
    public async Task<AiModelDto> UpdateAsync([FromRoute] Guid id, AiModelUpdateInput input)
    {
        var entity = await _modelRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new UserFriendlyException("模型不存在");
        }

        // 验证供应商是否存在
        if (entity.AiProviderId != input.AiProviderId)
        {
            var providerExists = await _providerRepository._DbQueryable
                .Where(x => x.Id == input.AiProviderId)
                .AnyAsync();

            if (!providerExists)
            {
                throw new UserFriendlyException("指定的AI供应商不存在");
            }
        }

        entity.HandlerName = input.HandlerName;
        entity.ModelId = input.ModelId;
        entity.Name = input.Name;
        entity.Description = input.Description;
        entity.OrderNum = input.OrderNum;
        entity.AiProviderId = input.AiProviderId;
        entity.ExtraInfo = input.ExtraInfo;
        entity.ModelType = input.ModelType;
        entity.ModelApiType = input.ModelApiType;
        entity.Multiplier = input.Multiplier;
        entity.MultiplierShow = input.MultiplierShow;
        entity.ProviderName = input.ProviderName;
        entity.IconUrl = input.IconUrl;
        entity.IsPremium = input.IsPremium;
        entity.IsEnabled = input.IsEnabled;

        await _modelRepository.UpdateAsync(entity);
        return entity.Adapt<AiModelDto>();
    }

    /// <summary>
    /// 删除AI模型
    /// </summary>
    [HttpDelete("ai-model/{id}")]
    public async Task DeleteAsync([FromRoute] Guid id)
    {
        await _modelRepository.DeleteByIdAsync(id);
    }
}
