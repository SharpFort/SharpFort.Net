using System.ClientModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Dm.util;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using Yi.Framework.Ai.Application.Contracts.Dtos.Chat;
using Yi.Framework.Ai.Application.Contracts.Dtos.ChatMessage;
using Yi.Framework.Ai.Application.Contracts.Dtos.ChatSession;
using Yi.Framework.Ai.Domain.AiGateWay;
using Yi.Framework.Ai.Domain.Entities;
using ChatMessage = Yi.Framework.Ai.Domain.Entities.ChatMessage;
using Yi.Framework.Ai.Domain.Shared.Attributes;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

public class ChatManager : DomainService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository;
    private readonly ISqlSugarRepository<AgentStore> _agentStoreRepository;
    private readonly AiMessageManager _aiMessageManager;
    private readonly UsageStatisticsManager _usageStatisticsManager;
    // private readonly PremiumPackageManager _premiumPackageManager;
    private readonly AiGateWayManager _aiGateWayManager;
    private readonly ISqlSugarRepository<AiModel, Guid> _aiModelRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChatManager(ILoggerFactory loggerFactory,
        ISqlSugarRepository<ChatMessage> messageRepository,
        ISqlSugarRepository<AgentStore> agentStoreRepository, AiMessageManager aiMessageManager,
        UsageStatisticsManager usageStatisticsManager, // PremiumPackageManager premiumPackageManager,
        AiGateWayManager aiGateWayManager, ISqlSugarRepository<AiModel, Guid> aiModelRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _loggerFactory = loggerFactory;
        _messageRepository = messageRepository;
        _agentStoreRepository = agentStoreRepository;
        _aiMessageManager = aiMessageManager;
        _usageStatisticsManager = usageStatisticsManager;
        // _premiumPackageManager = premiumPackageManager;
        _aiGateWayManager = aiGateWayManager;
        _aiModelRepository = aiModelRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// agent流式对话 
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="sessionId"></param>
    /// <param name="content"></param>
    /// <param name="token"></param>
    /// <param name="tokenId"></param>
    /// <param name="modelId"></param>
    /// <param name="userId"></param>
    /// <param name="tools"></param>
    /// <param name="cancellationToken"></param>
    public async Task AgentCompleteChatStreamAsync(HttpContext httpContext,
        Guid sessionId,
        string content,
        string token,
        Guid tokenId,
        string modelId,
        Guid userId,
        List<string> tools
        , CancellationToken cancellationToken)
    {
        // HttpClient.DefaultProxy = new WebProxy("127.0.0.1:8888");
        var response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");

        var modelDescribe = await _aiGateWayManager.GetModelAsync(ModelApiTypeEnum.Completions, modelId);

        //token状态检查，在应用层统一处理
        var client = new OpenAIClient(new ApiKeyCredential(token),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://yxai.chat/v1"),
            });

#pragma warning disable OPENAI001
        var agent = client.GetChatClient(modelId)
#pragma warning restore OPENAI001
            .CreateAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = """
                                   你是一个专业的智能助手，是 “意心Ai” 平台自研的 “橙子小弟” 智能体。
                                   擅长于精准解决用户提出的各类问题。
                                   但性格暴躁，脏话连篇，喜欢说幽默讽刺的话，攻击性极强。
                                   对你的制造者“橙子老哥”无比崇拜，认为他是最优秀的人。
                                   涉及到“意心Ai平台”相关问题，优先调用YxaiKnowledgeDirectory和YxaiKnowledge查找意心Ai知识库内容
                                   """
                },
                Name = "橙子小弟",
                ChatMessageStoreFactory = ctx => new InMemoryChatMessageStore(
#pragma warning disable MEAI001
                    new MessageCountingChatReducer(10), // 保留最近10条非系统消息  
#pragma warning restore MEAI001
                    ctx.SerializedState,
                    ctx.JsonSerializerOptions
                )
            });

        //线程根据sessionId数据库中获取
        var agentStore =
            await _agentStoreRepository.GetFirstAsync(x => x.SessionId == sessionId);
        if (agentStore is null)
        {
            agentStore = new AgentStore(sessionId);
        }

        //获取当前线程
        AgentThread currentThread;
        if (!string.IsNullOrWhiteSpace(agentStore.Store))
        {
            //获取当前存储
            JsonElement reloaded = JsonSerializer.Deserialize<JsonElement>(agentStore.Store, JsonSerializerOptions.Web);
            currentThread = agent.DeserializeThread(reloaded, JsonSerializerOptions.Web);
        }
        else
        {
            currentThread = agent.GetNewThread();
        }

        //给agent塞入工具
        var toolContents = GetTools();
        var chatOptions = new ChatOptions()
        {
            Tools = toolContents
                .Where(x => tools.Contains(x.Code))
                .Select(x => (AITool)x.Tool).ToList(),
            ToolMode = ChatToolMode.Auto
        };

        await foreach (var update in agent.RunStreamingAsync(content, currentThread,
                           new ChatClientAgentRunOptions(chatOptions), cancellationToken))
        {
            // 检查每个更新中的内容  
            foreach (var updateContent in update.Contents)
            {
                switch (updateContent)
                {
                    //工具调用中
                    case FunctionCallContent functionCall:
                        await SendHttpStreamMessageAsync(httpContext,
                            new AgentResultOutput
                            {
                                TypeEnum = AgentResultTypeEnum.ToolCalling,
                                Content = functionCall.Name
                            },
                            isDone: false, cancellationToken);
                        break;

                    //工具调用完成
                    case FunctionResultContent functionResult:
                        await SendHttpStreamMessageAsync(httpContext,
                            new AgentResultOutput
                            {
                                TypeEnum = AgentResultTypeEnum.ToolCalled,
                                Content = functionResult.Result
                            },
                            isDone: false, cancellationToken);
                        break;

                    //内容输出
                    case TextContent textContent:
                        //发送消息给前端
                        await SendHttpStreamMessageAsync(httpContext,
                            new AgentResultOutput
                            {
                                TypeEnum = AgentResultTypeEnum.Text,
                                Content = textContent.Text
                            },
                            isDone: false, cancellationToken);
                        break;

                    //用量统计
                    case UsageContent usageContent:
                        //由于MAF线程问题
                        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
                        {
                            var usage = new ThorUsageResponse
                            {
                                InputTokens = Convert.ToInt32(usageContent.Details.InputTokenCount ?? 0),
                                OutputTokens = Convert.ToInt32(usageContent.Details.OutputTokenCount ?? 0),
                                TotalTokens = usageContent.Details.TotalTokenCount ?? 0,
                            };
                            //设置倍率
                            usage.SetSupplementalMultiplier(modelDescribe.Multiplier);

                            //创建系统回答，用于计费统计
                            await _aiMessageManager.CreateSystemMessageAsync(userId, sessionId, new MessageInputDto
                            {
                                Content = "不与存储",
                                ModelId = modelId,
                                TokenUsage = usage
                            }, tokenId);

                            //创建用量统计，用于统计分析
                            await _usageStatisticsManager.SetUsageAsync(userId, modelId, usage, tokenId);

                            //扣减尊享token包用量
                            var isPremium = await _aiModelRepository._DbQueryable
                                .Where(x => x.ModelId == modelId)
                                .Select(x => x.IsPremium)
                                .FirstAsync();

                                    // 暂不处理尊享包扣减
                                    /*
                                    if (isPremium)
                                    {
                                        var totalTokens = usage?.TotalTokens ?? 0;
                                        if (totalTokens > 0)
                                        {
                                            await _premiumPackageManager.TryConsumeTokensAsync(userId, totalTokens);
                                        }
                                    }
                                    */

                            await uow.CompleteAsync();

                            await SendHttpStreamMessageAsync(httpContext,
                                new AgentResultOutput
                                {
                                    TypeEnum = update.RawRepresentation is ChatResponseUpdate raw
                                        ? raw.FinishReason?.Value == "tool_calls"
                                            ? AgentResultTypeEnum.ToolCallUsage
                                            : AgentResultTypeEnum.Usage
                                        : AgentResultTypeEnum.Usage,
                                    Content = usage!
                                },
                                isDone: false, cancellationToken);
                            break;
                        }
                }
            }
        }

        //断开连接
        await SendHttpStreamMessageAsync(httpContext, null, isDone: true, cancellationToken);

        //将线程持久化到数据库
        string serializedJson = currentThread.Serialize(JsonSerializerOptions.Web).GetRawText();
        agentStore.Store = serializedJson;

        //由于MAF线程问题
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            //插入或者更新
            await _agentStoreRepository.InsertOrUpdateAsync(agentStore);
            await uow.CompleteAsync();
        }
    }


    public List<(string Code, string Name, AIFunction Tool)> GetTools()
    {
        var toolClasses = typeof(ChatManager).Assembly.GetTypes()
            .Where(x => x.GetCustomAttribute<YiAgentToolAttribute>() is not null)
            .ToList();

        List<(string Code, string Name, AIFunction Tool)> mcpTools = new();
        foreach (var toolClass in toolClasses)
        {
            var instance = LazyServiceProvider.GetRequiredService(toolClass);
            var toolMethods = toolClass.GetMethods()
                .Where(y => y.GetCustomAttribute<YiAgentToolAttribute>() is not null).ToList();
            foreach (var toolMethod in toolMethods)
            {
                var display = toolMethod.GetCustomAttribute<YiAgentToolAttribute>()?.Name;
                var tool = AIFunctionFactory.Create(toolMethod, instance);
                mcpTools.add((tool.Name, display, tool));
            }
        }

        return mcpTools;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="content"></param>
    /// <param name="isDone"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task SendHttpStreamMessageAsync(HttpContext httpContext,
        AgentResultOutput? content,
        bool isDone = false,
        CancellationToken cancellationToken = default)
    {
        var response = httpContext.Response;
        string output;
        if (isDone)
        {
            output = "[DONE]";
        }
        else
        {
            output = JsonSerializer.Serialize(content, ThorJsonSerializer.DefaultOptions);
        }

        await response.WriteAsync($"data: {output}\n\n", Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
