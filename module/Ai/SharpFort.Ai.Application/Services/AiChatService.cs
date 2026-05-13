using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mapster;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos.AiModel;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Managers;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.CasbinRbac.Application.Contracts.IServices;

namespace SharpFort.Ai.Application.Services;

/// <summary>
/// AI聊天服务
/// </summary>
public class AiChatService(IHttpContextAccessor httpContextAccessor,
    AiBlacklistManager aiBlacklistManager,
    ILogger<AiChatService> logger,
    AiGateWayManager aiGateWayManager,
    ModelManager modelManager,
    ChatManager chatManager, TokenManager tokenManager, IAccountService accountService,
    ISqlSugarRepository<AgentStore> agentStoreRepository,
    ISqlSugarRepository<AiModel> aiModelRepository) : ApplicationService, IAiChatService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly AiBlacklistManager _aiBlacklistManager = aiBlacklistManager;
    private readonly ILogger<AiChatService> _logger = logger;
    private readonly AiGateWayManager _aiGateWayManager = aiGateWayManager;
    private readonly ModelManager _modelManager = modelManager;
    private readonly ChatManager _chatManager = chatManager;
    private readonly TokenManager _tokenManager = tokenManager;
    private readonly IAccountService _accountService = accountService;
    private readonly ISqlSugarRepository<AgentStore> _agentStoreRepository = agentStoreRepository;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository = aiModelRepository;
    private const string FreeModelId = "DeepSeek-V3-0324"; // Keep constant or move to config

    /// <summary>
    /// 获取可用的对话模型列表
    /// </summary>
    public async Task<List<AiModelDto>> GetModelListAsync()
    {
        List<AiModel> entities = await _aiModelRepository._DbQueryable
            .Where(x => x.IsEnabled)
            .Where(x => x.ModelType == ModelType.Chat)
            .OrderByDescending(x => x.OrderNum)
            .ToListAsync();

        List<AiModelDto> output = entities.Adapt<List<AiModelDto>>();

        // Custom logic for free model if needed, adapted from original
        output.ForEach(x =>
        {
            if (x.ModelId == FreeModelId)
            {
                // x.IsFree = true; // DTO doesn't have IsFree?
            }
        });

        return output;
    }

    /// <summary>
    /// 统一发送消息 - 支持多种API类型
    /// </summary>
    public async Task UnifiedSendAsync(
        ModelApiType apiType,
        JsonElement input,
        string modelId,
        Guid? sessionId)
    {
        // 从请求体中提取模型ID（如果未从URL传入）
        if (string.IsNullOrEmpty(modelId))
        {
            modelId = ExtractModelIdFromRequest(input);
        }

        // 除了免费模型，其他的模型都要校验
        if (modelId != FreeModelId)
        {
            if (CurrentUser.IsAuthenticated)
            {
                await _aiBlacklistManager.VerifiyAiBlacklist(CurrentUser.GetId());
            }
        }

        // 调用统一流式处理
        await _aiGateWayManager.UnifiedStreamForStatisticsAsync(
            _httpContextAccessor.HttpContext!,
            apiType,
            input,
            modelId,
            CurrentUser.Id,
            sessionId,
            null,
            CancellationToken.None);
    }

    /// <summary>
    /// 从请求体中提取模型ID
    /// </summary>
    private static string ExtractModelIdFromRequest(JsonElement input)
    {
        try
        {
            if (input.TryGetProperty("model", out JsonElement modelProperty))
            {
                return modelProperty.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // 忽略解析错误
        }

        // throw new UserFriendlyException("无法从请求中获取模型ID，请在URL参数中指定modelId");
        return string.Empty; // Allow empty if allowed downstream
    }
}
