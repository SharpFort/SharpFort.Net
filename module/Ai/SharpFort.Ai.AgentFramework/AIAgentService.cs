using HttpMataki.NET.Auto;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using SharpFort.Ai.AgentFramework.Interfaces;
using System.ClientModel;

namespace SharpFort.Ai.AgentFramework;

/// <summary>
/// AI代理服务 - 核心引擎
/// </summary>
public class AIAgentService : IAIAgentService
{
    private readonly ILogger<AIAgentService> _logger;

    public AIAgentService(ILogger<AIAgentService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 创建OpenAI代理并发送消息
    /// </summary>
    public async Task<(AIAgent, string)> CreateOpenAIAgentAndSendMSG(
        AISetting aISetting,
        ChatClientAgentOptions chatClientAgentOptions,
        string msg)
    {
        if (aISetting.IsHttpLog)
        {
            HttpClientAutoInterceptor.StartInterception();
        }

        var openAIClientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(aISetting.AIUrl),
            NetworkTimeout = TimeSpan.FromMinutes(aISetting.NetworkTimeout),
            RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(maxRetries: aISetting.MaxRetries)
        };

        // AI工具开关
        if (!aISetting.IsAITools)
        {
            if (chatClientAgentOptions.ChatOptions is not null)
            {
                chatClientAgentOptions.ChatOptions.Tools = [];
            }
        }

        // AI技能开关
        if (!aISetting.IsAISkills)
        {
            chatClientAgentOptions.AIContextProviders = null;
        }

        var ai = new OpenAIClient(
            new ApiKeyCredential(string.IsNullOrWhiteSpace(aISetting.AIKeySecret) ? "local" : aISetting.AIKeySecret),
            openAIClientOptions);

        var aiAgent = ai.GetChatClient(aISetting.AIDefaultModel)
            .AsIChatClient()
            .AsAIAgent(chatClientAgentOptions);

        var resultText = string.Empty;

        if (aISetting.IsStreame)
        {
            if (aISetting.StreameCallback is not null)
            {
                await foreach (var update in aiAgent.RunStreamingAsync(msg))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        aISetting.StreameCallback.Invoke(update.Text);
                        resultText += update.Text;
                    }
                }
            }
        }
        else
        {
            var result = await aiAgent.RunAsync(msg);
            resultText = result.Text;
        }

        if (aISetting.IsHttpLog)
        {
            HttpClientAutoInterceptor.StopInterception();
        }

        return (aiAgent, resultText);
    }
}
