using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mapster;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiModel;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;
using Yi.Framework.Rbac.Application.Contracts.IServices;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// AI聊天服务
/// </summary>
public class AiChatService : ApplicationService, IAiChatService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AiBlacklistManager _aiBlacklistManager;
    private readonly ILogger<AiChatService> _logger;
    private readonly AiGateWayManager _aiGateWayManager;
    private readonly ModelManager _modelManager;
    // private readonly PremiumPackageManager _premiumPackageManager; // Omitted for now
    private readonly ChatManager _chatManager;
    private readonly TokenManager _tokenManager;
    private readonly IAccountService _accountService;
    private readonly ISqlSugarRepository<AgentStore> _agentStoreRepository;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository;
    private const string FreeModelId = "DeepSeek-V3-0324"; // Keep constant or move to config

    public AiChatService(IHttpContextAccessor httpContextAccessor,
        AiBlacklistManager aiBlacklistManager,
        ILogger<AiChatService> logger,
        AiGateWayManager aiGateWayManager,
        ModelManager modelManager,
        // PremiumPackageManager premiumPackageManager,
        ChatManager chatManager, TokenManager tokenManager, IAccountService accountService,
        ISqlSugarRepository<AgentStore> agentStoreRepository,
        ISqlSugarRepository<AiModel> aiModelRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _aiBlacklistManager = aiBlacklistManager;
        _logger = logger;
        _aiGateWayManager = aiGateWayManager;
        _modelManager = modelManager;
        // _premiumPackageManager = premiumPackageManager;
        _chatManager = chatManager;
        _tokenManager = tokenManager;
        _accountService = accountService;
        _agentStoreRepository = agentStoreRepository;
        _aiModelRepository = aiModelRepository;
    }

    /// <summary>
    /// 获取可用的对话模型列表
    /// </summary>
    public async Task<List<AiModelDto>> GetModelListAsync()
    {
        var entities = await _aiModelRepository._DbQueryable
            .Where(x => x.IsEnabled == true)
            .Where(x => x.ModelType == ModelTypeEnum.Chat)
            .OrderByDescending(x => x.OrderNum)
            .ToListAsync();

        var output = entities.Adapt<List<AiModelDto>>();
        
        // Custom logic for free model if needed, adapted from original
        output.ForEach(x =>
        {
            if (x.ModelId == FreeModelId)
            {
                // x.IsPremium = false; // logic from original
                // x.IsFree = true; // DTO doesn't have IsFree?
            }
        });

        return output;
    }

    /// <summary>
    /// 统一发送消息 - 支持多种API类型
    /// </summary>
    public async Task UnifiedSendAsync(
        ModelApiTypeEnum apiType,
        JsonElement input,
        string modelId,
        Guid? sessionId)
    {
        // 从请求体中提取模型ID（如果未从URL传入）
        if (string.IsNullOrEmpty(modelId))
        {
            modelId = ExtractModelIdFromRequest(apiType, input);
        }

        // 除了免费模型，其他的模型都要校验
        if (modelId != FreeModelId)
        {
            if (CurrentUser.IsAuthenticated)
            {
                await _aiBlacklistManager.VerifiyAiBlacklist(CurrentUser.GetId());
                // VIP Check logic - simplified or via IAccountService
                // var userInfo = await _accountService.GetAsync();
                // if (!userInfo.RoleCodes.Contains("vip")) ...
            }
            else
            {
                 // Allow anonymous for now or throw? Original threw exception.
                 // throw new UserFriendlyException("未登录用户，只能使用未加速的DeepSeek-R1，请登录后重试");
            }
        }

        // 尊享包校验逻辑 (Omitted)

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
    private string ExtractModelIdFromRequest(ModelApiTypeEnum apiType, JsonElement input)
    {
        try
        {
            if (input.TryGetProperty("model", out var modelProperty))
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
