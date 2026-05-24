using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharpFort.Ai.AgentFramework.Interfaces.Tasks;

namespace SharpFort.Ai.Application.Controllers;

/// <summary>
/// AI自动任务管理
/// </summary>
[Route("api/ai-task")]
[ApiController]
[Authorize]
public class AiTaskController : ControllerBase
{
    private readonly IKevinAITaskService _service;

    public AiTaskController(IKevinAITaskService service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取我的AI任务列表
    /// </summary>
    [HttpGet("list")]
    public async Task<List<string>> GetTaskListAsync()
    {
        return await _service.GetTaskList();
    }

    /// <summary>
    /// 创建或更新Cron任务
    /// </summary>
    [HttpPost]
    public async Task<string> AddOrUpdateCronTaskAsync(
        [FromQuery] string name,
        [FromQuery] string content,
        [FromQuery] string cronExpression)
    {
        return await _service.AddOrUpdateCronTask(name, content, cronExpression);
    }

    /// <summary>
    /// 删除我的AI任务
    /// </summary>
    [HttpDelete]
    public async Task<string> RemoveCronTaskAsync([FromQuery] string name)
    {
        return await _service.RemoveCronTask(name);
    }

    /// <summary>
    /// 立即执行我的AI任务
    /// </summary>
    [HttpPost("trigger")]
    public async Task<string> TriggerCronTaskAsync([FromQuery] string name)
    {
        return await _service.TriggerCronTask(name);
    }
}
