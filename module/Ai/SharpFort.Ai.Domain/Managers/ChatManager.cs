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

/*
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
        // 留白，等待 Phase 3 使用原生 OpenAI 重构
    }


    public List<(string Code, string? Name, object Tool)> GetTools()
    {
        return new List<(string Code, string? Name, object Tool)>();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    private static async Task SendHttpStreamMessageAsync(HttpContext httpContext,
        AgentResultOutput? content,
        bool isDone = false,
        CancellationToken cancellationToken = default)
    {
        
    }
*/
}
