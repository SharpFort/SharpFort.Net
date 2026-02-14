using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiPrompt;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// AI提示词管理服务
/// </summary>
[Authorize]
public class AiPromptService : ApplicationService
{
    private readonly ISqlSugarRepository<AiPrompt> _repository;

    public AiPromptService(ISqlSugarRepository<AiPrompt> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 获取提示词列表
    /// </summary>
    [HttpGet("ai-prompt")]
    public async Task<PagedResultDto<AiPromptDto>> GetListAsync(AiPromptGetListInput input)
    {
        RefAsync<int> total = 0;

        var entities = await _repository._DbQueryable
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), x => x.Code.Contains(input.SearchKey) || x.Content.Contains(input.SearchKey) || x.Description.Contains(input.SearchKey))
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        var output = entities.Adapt<List<AiPromptDto>>();
        return new PagedResultDto<AiPromptDto>(total, output);
    }

    /// <summary>
    /// 根据ID获取提示词
    /// </summary>
    [HttpGet("ai-prompt/{id}")]
    public async Task<AiPromptDto> GetAsync([FromRoute] Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity.Adapt<AiPromptDto>();
    }

    /// <summary>
    /// 创建提示词
    /// </summary>
    [HttpPost("ai-prompt")]
    public async Task<AiPromptDto> CreateAsync(AiPromptCreateInput input)
    {
        var entity = input.Adapt<AiPrompt>();
        await _repository.InsertAsync(entity);
        return entity.Adapt<AiPromptDto>();
    }

    /// <summary>
    /// 更新提示词
    /// </summary>
    [HttpPut("ai-prompt/{id}")]
    public async Task<AiPromptDto> UpdateAsync([FromRoute] Guid id, AiPromptUpdateInput input)
    {
        var entity = await _repository.GetByIdAsync(id);
        input.Adapt(entity);
        await _repository.UpdateAsync(entity);
        return entity.Adapt<AiPromptDto>();
    }

    /// <summary>
    /// 删除提示词
    /// </summary>
    [HttpDelete("ai-prompt/{id}")]
    public async Task DeleteAsync([FromRoute] Guid id)
    {
        await _repository.DeleteAsync(x => x.Id == id);
    }
}
