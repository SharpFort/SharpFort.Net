using Microsoft.Agents.AI;

namespace SharpFort.Ai.AgentFramework.Interfaces;

public interface IAIAgentService
{
    /// <summary>
    /// 创建代理并发送消息
    /// </summary>
    Task<(AIAgent, string)> CreateOpenAIAgentAndSendMSG(
        AISetting aISetting,
        ChatClientAgentOptions chatClientAgentOptions,
        string msg);
}
