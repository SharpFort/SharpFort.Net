using System.ClientModel;
using System.Globalization;
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
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;
using SharpFort.Ai.Application.Contracts.Dtos.Chat;
using SharpFort.Ai.Domain.AiGateWay;
using SharpFort.Ai.Domain.Entities;
using ChatMessage = SharpFort.Ai.Domain.Entities.ChatMessage;
using SharpFort.Ai.Domain.Shared.Attributes;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Shared.Dtos.OpenAi;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

public class ChatManager(ILoggerFactory loggerFactory,
    ISqlSugarRepository<ChatMessage> messageRepository,
    ISqlSugarRepository<AgentStore> agentStoreRepository, AiMessageManager aiMessageManager,
    UsageStatisticsManager usageStatisticsManager,
    AiGateWayManager aiGateWayManager, ISqlSugarRepository<AiModel, Guid> aiModelRepository,
    IUnitOfWorkManager unitOfWorkManager) : DomainService
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository = messageRepository;
    private readonly ISqlSugarRepository<AgentStore> _agentStoreRepository = agentStoreRepository;
    private readonly AiMessageManager _aiMessageManager = aiMessageManager;
    private readonly UsageStatisticsManager _usageStatisticsManager = usageStatisticsManager;
    // private readonly PremiumPackageManager _premiumPackageManager;
    private readonly AiGateWayManager _aiGateWayManager = aiGateWayManager;
    private readonly ISqlSugarRepository<AiModel, Guid> _aiModelRepository = aiModelRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;

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
        HttpResponse response = httpContext.Response;
        // 设置响应头，声明是 SSE 流
        response.ContentType = "text/event-stream;charset=utf-8;";
        response.Headers.TryAdd("Cache-Control", "no-cache");
        response.Headers.TryAdd("Connection", "keep-alive");

        AiModelDescribe modelDescribe = await _aiGateWayManager.GetModelAsync(ModelApiType.Completions, modelId);

        //token状态检查，在应用层统一处理
        OpenAIClient client = new(new ApiKeyCredential(token),
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
        AgentStore agentStore =
            await _agentStoreRepository.GetFirstAsync(x => x.SessionId == sessionId) ?? new AgentStore(sessionId);

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
        List<(string Code, string? Name, AIFunction Tool)> toolContents = GetTools();
        ChatOptions chatOptions = new()
        {
            Tools = [.. toolContents
                .Where(x => tools.Contains(x.Code))
                .Select(x => (AITool)x.Tool)],
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
                                Content = functionResult.Result!
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
                        using (IUnitOfWork uow = _unitOfWorkManager.Begin(requiresNew: true))
                        {
                            ThorUsageResponse usage = new()
                            {
                                InputTokens = Convert.ToInt32(usageContent.Details.InputTokenCount ?? 0, CultureInfo.InvariantCulture),
                                OutputTokens = Convert.ToInt32(usageContent.Details.OutputTokenCount ?? 0, CultureInfo.InvariantCulture),
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



                            await uow.CompleteAsync(cancellationToken);

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

                    default:
                        break;
                }
            }
        }

        //断开连接
        await SendHttpStreamMessageAsync(httpContext, null, isDone: true, cancellationToken);

        //将线程持久化到数据库
        string serializedJson = currentThread.Serialize(JsonSerializerOptions.Web).GetRawText();
        agentStore.Store = serializedJson;

        //由于MAF线程问题
        using (IUnitOfWork uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            //插入或者更新
            await _agentStoreRepository.InsertOrUpdateAsync(agentStore);
            await uow.CompleteAsync(cancellationToken);
        }
    }


    public List<(string Code, string? Name, AIFunction Tool)> GetTools()
    {
        List<Type> toolClasses = [.. typeof(ChatManager).Assembly.GetTypes().Where(x => x.GetCustomAttribute<SfAgentToolAttribute>() is not null)];

        List<(string Code, string? Name, AIFunction Tool)> mcpTools = [];
        foreach (Type? toolClass in toolClasses)
        {
            object instance = LazyServiceProvider.GetRequiredService(toolClass);
            List<MethodInfo> toolMethods = [.. toolClass.GetMethods().Where(y => y.GetCustomAttribute<SfAgentToolAttribute>() is not null)];
            foreach (MethodInfo? toolMethod in toolMethods)
            {
                string? display = toolMethod.GetCustomAttribute<SfAgentToolAttribute>()?.Name;
                AIFunction tool = AIFunctionFactory.Create(toolMethod, instance);
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
    private static async Task SendHttpStreamMessageAsync(HttpContext httpContext,
        AgentResultOutput? content,
        bool isDone = false,
        CancellationToken cancellationToken = default)
    {
        HttpResponse response = httpContext.Response;
        string output;
        output = isDone ? "[DONE]" : JsonSerializer.Serialize(content, ThorJsonSerializer.DefaultOptions);

        await response.WriteAsync($"data: {output}\n\n", Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
