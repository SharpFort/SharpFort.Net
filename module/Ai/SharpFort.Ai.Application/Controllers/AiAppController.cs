using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using SharpFort.Ai.Application.Contracts.Dtos.AiApp;
using SharpFort.Ai.Application.Contracts.IServices;

namespace SharpFort.Ai.Application.Controllers;

/// <summary>
/// AI应用管理
/// </summary>
[Route("api/ai-app")]
[ApiController]
[Authorize]
public class AiAppController : ControllerBase
{
    private readonly IAiAppService _service;

    public AiAppController(IAiAppService service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取AI应用分页列表
    /// </summary>
    [HttpPost("list")]
    public async Task<PagedResultDto<AiAppDto>> GetListAsync([FromBody] AiAppGetListInput input)
    {
        return await _service.GetListAsync(input);
    }

    /// <summary>
    /// 获取AI应用全部列表
    /// </summary>
    [HttpGet("all")]
    public async Task<List<AiAppDto>> GetAllListAsync()
    {
        return await _service.GetAllListAsync();
    }

    /// <summary>
    /// 获取AI应用详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<AiAppDto> GetAsync([FromRoute][Required] Guid id)
    {
        return await _service.GetAsync(id);
    }

    /// <summary>
    /// 新增AI应用
    /// </summary>
    [HttpPost]
    public async Task<AiAppDto> CreateAsync([FromBody] AiAppDto input)
    {
        return await _service.CreateAsync(input);
    }

    /// <summary>
    /// 编辑AI应用
    /// </summary>
    [HttpPut("{id}")]
    public async Task<AiAppDto> UpdateAsync([FromRoute][Required] Guid id, [FromBody] AiAppDto input)
    {
        return await _service.UpdateAsync(id, input);
    }

    /// <summary>
    /// 删除AI应用
    /// </summary>
    [HttpDelete("{id}")]
    public async Task DeleteAsync([FromRoute][Required] Guid id)
    {
        await _service.DeleteAsync(id);
    }

    /// <summary>
    /// 智能体初始化
    /// </summary>
    [HttpGet("new-initialization")]
    public async Task<AiAppDto> NewInitializationAsync()
    {
        return await _service.NewInitializationAsync();
    }
}
