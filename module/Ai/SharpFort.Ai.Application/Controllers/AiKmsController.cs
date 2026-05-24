using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharpFort.Ai.Application.Contracts.Dtos.AiKms;
using SharpFort.Ai.Application.Contracts.IServices;
using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Controllers;

/// <summary>
/// AI知识库管理
/// </summary>
[Route("api/ai-kms")]
[ApiController]
[Authorize]
public class AiKmsController : ControllerBase
{
    private readonly IAiKmsService _service;

    public AiKmsController(IAiKmsService service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取知识库分页列表
    /// </summary>
    [HttpPost("list")]
    public async Task<PagedResultDto<AiKmsDto>> GetListAsync([FromBody] PagedAndSortedResultRequestDto input)
    {
        return await _service.GetListAsync(input);
    }

    /// <summary>
    /// 获取知识库全部列表
    /// </summary>
    [HttpGet("all")]
    public async Task<List<AiKmsDto>> GetAllListAsync()
    {
        return await _service.GetAllListAsync();
    }

    /// <summary>
    /// 获取知识库详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<AiKmsDto> GetAsync([FromRoute][Required] Guid id)
    {
        return await _service.GetAsync(id);
    }

    /// <summary>
    /// 新增知识库
    /// </summary>
    [HttpPost]
    public async Task<AiKmsDto> CreateAsync([FromBody] AiKmsDto input)
    {
        return await _service.CreateAsync(input);
    }

    /// <summary>
    /// 编辑知识库
    /// </summary>
    [HttpPut("{id}")]
    public async Task<AiKmsDto> UpdateAsync([FromRoute][Required] Guid id, [FromBody] AiKmsDto input)
    {
        return await _service.UpdateAsync(id, input);
    }

    /// <summary>
    /// 删除知识库
    /// </summary>
    [HttpDelete("{id}")]
    public async Task DeleteAsync([FromRoute][Required] Guid id)
    {
        await _service.DeleteAsync(id);
    }
}
