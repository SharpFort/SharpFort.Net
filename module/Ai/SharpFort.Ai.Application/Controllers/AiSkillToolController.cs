using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharpFort.Ai.Application.Contracts.Dtos.AiSkillTool;
using SharpFort.Ai.Application.Contracts.IServices;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Controllers;

/// <summary>
/// AI技能/工具管理
/// </summary>
[Route("api/ai-skill-tool")]
[ApiController]
[Authorize]
public class AiSkillToolController : ControllerBase
{
    private readonly IAiSkillToolService _service;

    public AiSkillToolController(IAiSkillToolService service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取技能/工具分页列表
    /// </summary>
    [HttpPost("list")]
    public async Task<PagedResultDto<AiSkillToolDto>> GetListAsync([FromBody] PagedAndSortedResultRequestDto input)
    {
        return await _service.GetListAsync(input);
    }

    /// <summary>
    /// 获取所有Skill技能
    /// </summary>
    [HttpGet("skills")]
    public async Task<List<AiSkillToolDto>> GetAllSkillsAsync()
    {
        return await _service.GetAllSkillsAsync();
    }

    /// <summary>
    /// 获取所有Tool工具
    /// </summary>
    [HttpGet("tools")]
    public async Task<List<AiSkillToolDto>> GetAllToolsAsync()
    {
        return await _service.GetAllToolsAsync();
    }

    /// <summary>
    /// 新增技能/工具
    /// </summary>
    [HttpPost]
    public async Task<AiSkillToolDto> CreateAsync([FromBody] AiSkillToolDto input)
    {
        return await _service.CreateAsync(input);
    }

    /// <summary>
    /// 编辑技能/工具
    /// </summary>
    [HttpPut("{id}")]
    public async Task<AiSkillToolDto> UpdateAsync([FromRoute][Required] Guid id, [FromBody] AiSkillToolDto input)
    {
        return await _service.UpdateAsync(id, input);
    }

    /// <summary>
    /// 删除技能/工具
    /// </summary>
    [HttpDelete("{id}")]
    public async Task DeleteAsync([FromRoute][Required] Guid id)
    {
        await _service.DeleteAsync(id);
    }
}
