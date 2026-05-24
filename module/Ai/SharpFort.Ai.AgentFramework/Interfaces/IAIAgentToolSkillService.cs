using Microsoft.Extensions.AI;

namespace SharpFort.Ai.AgentFramework.Interfaces;

public interface IAIAgentToolSkillService
{
    Task<List<AITool>> GetAIAgentToolsAsync(object data, string agentId);
    Task<List<AITool>> GetUserAIAgentToolsAsync(object data, string agentId, string userId);
    Task<List<AITool>> GetAllAIAgentToolsAsync(object data);
    Task<List<string>> GetAIAgentSkillsAsync(object data, string agentId);
    Task<List<string>> GetUserAIAgentSkillsAsync(object data, string agentId, string userId);
    Task<List<string>> GetAllAIAgentSkillsAsync(object data);
}
