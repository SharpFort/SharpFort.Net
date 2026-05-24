using Microsoft.Extensions.AI;
using SharpFort.Ai.AgentFramework.Const;
using SharpFort.Ai.AgentFramework.Interfaces;
using SharpFort.Ai.AgentFramework.Interfaces.Tasks;
using SharpFort.Ai.Application.Contracts.IServices;
using Volo.Abp.Application.Services;

namespace SharpFort.Ai.Application.Services;

public class AiAgentToolSkillService : ApplicationService, IAIAgentToolSkillService
{
    private readonly IKevinAITaskService? _kevinAITaskService;
    private readonly IAiSkillToolBindService _skillToolBindService;
    private readonly IAiSkillToolService _skillToolService;

    public AiAgentToolSkillService(
        IAiSkillToolBindService skillToolBindService,
        IAiSkillToolService skillToolService,
        IKevinAITaskService? kevinAITaskService = null)
    {
        _skillToolBindService = skillToolBindService;
        _skillToolService = skillToolService;
        _kevinAITaskService = kevinAITaskService;
    }

    public async Task<List<AITool>> GetAIAgentToolsAsync(object data, string agentId)
    {
        var boundIds = await _skillToolBindService.GetBoundSkillToolIdsAsync(agentId);
        var tools = await _skillToolService.GetAllToolsAsync();
        var toolNames = tools.Where(t => boundIds.Contains(t.Id))
            .Select(t => t.ClassMethod ?? "").ToList();
        return await BuildToolListAsync(data, toolNames);
    }

    public async Task<List<AITool>> GetUserAIAgentToolsAsync(object data, string agentId, string userId)
    {
        return await GetAIAgentToolsAsync(data, agentId);
    }

    public async Task<List<AITool>> GetAllAIAgentToolsAsync(object data)
    {
        var tools = await _skillToolService.GetAllToolsAsync();
        return await BuildToolListAsync(data, tools.Select(t => t.ClassMethod ?? "").ToList());
    }

    public async Task<List<string>> GetAIAgentSkillsAsync(object data, string agentId)
    {
        var boundIds = await _skillToolBindService.GetBoundSkillToolIdsAsync(agentId);
        var skills = await _skillToolService.GetAllSkillsAsync();
        return skills.Where(t => boundIds.Contains(t.Id)).Select(t => t.Name ?? "").ToList();
    }

    public async Task<List<string>> GetUserAIAgentSkillsAsync(object data, string agentId, string userId)
    {
        return await GetAIAgentSkillsAsync(data, agentId);
    }

    public async Task<List<string>> GetAllAIAgentSkillsAsync(object data)
    {
        var skills = await _skillToolService.GetAllSkillsAsync();
        return skills.Select(t => t.Name ?? "").ToList();
    }

    private Task<List<AITool>> BuildToolListAsync(object data, List<string> toolNames)
    {
        var aiTools = new List<AITool>();
        _kevinAITaskService?.InitData(data);
        foreach (var name in toolNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (SysTools.Tools.TryGetValue(name, out var staticTool))
            {
                aiTools.Add(staticTool);
            }
            else if (_kevinAITaskService is not null)
            {
                switch (name)
                {
                    case "iKevinAITasksService.AddOrUpdateCronTask":
                        aiTools.Add(AIFunctionFactory.Create(_kevinAITaskService.AddOrUpdateCronTask,
                            new AIFunctionFactoryOptions { Name = "AddOrUpdateCronTask", Description = "创建或更新一个周期性自动任务" }));
                        break;
                    case "iKevinAITasksService.RemoveCronTask":
                        aiTools.Add(AIFunctionFactory.Create(_kevinAITaskService.RemoveCronTask,
                            new AIFunctionFactoryOptions { Name = "RemoveCronTask", Description = "移除周期性任务" }));
                        break;
                    case "iKevinAITasksService.TriggerCronTask":
                        aiTools.Add(AIFunctionFactory.Create(_kevinAITaskService.TriggerCronTask,
                            new AIFunctionFactoryOptions { Name = "TriggerCronTask", Description = "立即触发某个周期性任务一次" }));
                        break;
                    case "iKevinAITasksService.GetTaskList":
                        aiTools.Add(AIFunctionFactory.Create(_kevinAITaskService.GetTaskList,
                            new AIFunctionFactoryOptions { Name = "GetTaskList", Description = "获取我的所有周期性任务列表" }));
                        break;
                }
            }
        }
        return Task.FromResult(aiTools);
    }
}
