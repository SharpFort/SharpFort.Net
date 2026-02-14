using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiProvider;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// AI供应商管理服务
/// </summary>
[Authorize(Roles = "admin")]
public class AiProviderService : ApplicationService, IAiProviderService
{
    private readonly ISqlSugarRepository<AiProvider, Guid> _providerRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;

    public AiProviderService(
        ISqlSugarRepository<AiProvider, Guid> providerRepository,
        ISqlSugarRepository<AiModel, Guid> modelRepository)
    {
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
    }

    /// <summary>
    /// 获取AI供应商列表
    /// </summary>
    [HttpGet("ai-provider")]
    public async Task<PagedResultDto<AiProviderDto>> GetListAsync(AiProviderGetListInput input)
    {
        RefAsync<int> total = 0;

        var entities = await _providerRepository._DbQueryable
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), x => x.Name.Contains(input.SearchKey))
            .OrderByDescending(x => x.OrderNum)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        var output = entities.Adapt<List<AiProviderDto>>();
        return new PagedResultDto<AiProviderDto>(total, output);
    }

    /// <summary>
    /// 根据ID获取AI供应商
    /// </summary>
    [HttpGet("ai-provider/{id}")]
    public async Task<AiProviderDto> GetAsync([FromRoute] Guid id)
    {
        var entity = await _providerRepository.GetByIdAsync(id);
        return entity.Adapt<AiProviderDto>();
    }

    /// <summary>
    /// 创建AI供应商
    /// </summary>
    [HttpPost("ai-provider")]
    public async Task<AiProviderDto> CreateAsync(AiProviderCreateInput input)
    {
        var entity = new AiProvider
        {
            Name = input.Name,
            Endpoint = input.Endpoint,
            ExtraUrl = input.ExtraUrl,
            ApiKey = input.ApiKey,
            OrderNum = input.OrderNum
        };

        await _providerRepository.InsertAsync(entity);
        return entity.Adapt<AiProviderDto>();
    }

    /// <summary>
    /// 更新AI供应商
    /// </summary>
    [HttpPut("ai-provider/{id}")]
    public async Task<AiProviderDto> UpdateAsync([FromRoute] Guid id, AiProviderUpdateInput input)
    {
        var entity = await _providerRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new UserFriendlyException("供应商不存在");
        }

        entity.Name = input.Name;
        entity.Endpoint = input.Endpoint;
        entity.ExtraUrl = input.ExtraUrl;
        entity.ApiKey = input.ApiKey;
        entity.OrderNum = input.OrderNum;

        await _providerRepository.UpdateAsync(entity);
        return entity.Adapt<AiProviderDto>();
    }

    /// <summary>
    /// 删除AI供应商
    /// </summary>
    [HttpDelete("ai-provider/{id}")]
    public async Task DeleteAsync([FromRoute] Guid id)
    {
        // 检查是否有关联的模型
        var hasModels = await _modelRepository._DbQueryable
            .Where(x => x.AiProviderId == id && !x.IsDeleted)
            .AnyAsync();

        if (hasModels)
        {
            throw new UserFriendlyException("该供应商下存在模型,无法删除");
        }

        await _providerRepository.DeleteAsync(id);
    }
}
