using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Extensions;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Consts;
using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Images;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Responses;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

public class OpenApiService : ApplicationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OpenApiService> _logger;
    private readonly TokenManager _tokenManager;
    private readonly AiGateWayManager _aiGateWayManager;
    private readonly ModelManager _modelManager;
    private readonly AiBlacklistManager _aiBlacklistManager;
    // private readonly PremiumPackageManager _premiumPackageManager;
    private readonly ISqlSugarRepository<ImageStoreTaskAggregateRoot> _imageStoreRepository;
    private readonly ISqlSugarRepository<AiModel> _aiModelRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public OpenApiService(IHttpContextAccessor httpContextAccessor, ILogger<OpenApiService> logger,
        TokenManager tokenManager, AiGateWayManager aiGateWayManager,
        ModelManager modelManager, AiBlacklistManager aiBlacklistManager,
         /* PremiumPackageManager premiumPackageManager, */ ISqlSugarRepository<ImageStoreTaskAggregateRoot> imageStoreRepository, ISqlSugarRepository<AiModel> aiModelRepository,
        IServiceScopeFactory serviceScopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _tokenManager = tokenManager;
        _aiGateWayManager = aiGateWayManager;
        _modelManager = modelManager;
        _aiBlacklistManager = aiBlacklistManager;
        // _premiumPackageManager = premiumPackageManager;
        _imageStoreRepository = imageStoreRepository;
        _aiModelRepository = aiModelRepository;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// 对话
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1/chat/completions")]
    public async Task ChatCompletionsAsync([FromBody] ThorChatCompletionsRequest input,
        CancellationToken cancellationToken)
    {
        //前面都是校验，后面才是真正的调用
        var httpContext = this._httpContextAccessor.HttpContext;
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);



        //ai网关代理httpcontext
        if (input.Stream == true)
        {
            await _aiGateWayManager.CompleteChatStreamForStatisticsAsync(_httpContextAccessor.HttpContext, input,
                userId, null, tokenId,CancellationToken.None);
        }
        else
        {
            await _aiGateWayManager.CompleteChatForStatisticsAsync(_httpContextAccessor.HttpContext, input, userId,
                null, tokenId,
                CancellationToken.None);
        }

        // 记录请求日志
        if (tokenValidation.IsEnableLog)
        {
            FireAndForgetMessageLog(JsonSerializer.Serialize(input), tokenValidation.Token, tokenValidation.TokenName, input.Model, ModelApiTypeEnum.Completions);
        }
    }


    /// <summary>
    /// 图片生成
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1/images/generations")]
    public async Task ImagesGenerationsAsync([FromBody] ImageCreateRequest input, CancellationToken cancellationToken)
    {
        var httpContext = this._httpContextAccessor.HttpContext;
        Intercept(httpContext);
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);
        await _aiGateWayManager.CreateImageForStatisticsAsync(httpContext, userId, null, input, tokenId);
    }


    /// <summary>
    /// 向量生成
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1/embeddings")]
    public async Task EmbeddingAsync([FromBody] ThorEmbeddingInput input, CancellationToken cancellationToken)
    {
        var httpContext = this._httpContextAccessor.HttpContext;
        Intercept(httpContext);
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);
        await _aiGateWayManager.EmbeddingForStatisticsAsync(httpContext, userId, null, input, tokenId);
    }


    /// <summary>
    /// 获取模型列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("openApi/v1/models")]
    public async Task<ModelsListDto> ModelsAsync()
    {
        var data = await _aiModelRepository._DbQueryable
            .Where(x => x.ModelType == ModelTypeEnum.Chat)
            .OrderByDescending(x => x.OrderNum)
            .Select(x => new ModelsDataDto
            {
                Id = x.ModelId,
                @object = "model",
                Created = DateTime.Now.ToUnixTimeSeconds(),
                OwnedBy = "organization-owner",
                Type = x.ModelId
            }).ToListAsync();

        return new ModelsListDto()
        {
            Data = data
        };
    }


    /// <summary>
    /// Anthropic对话
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1/messages")]
    public async Task MessagesAsync([FromBody] AnthropicInput input,
        CancellationToken cancellationToken)
    {
        //前面都是校验，后面才是真正的调用
        var httpContext = this._httpContextAccessor.HttpContext;
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);



        //ai网关代理httpcontext
        if (input.Stream)
        {
            await _aiGateWayManager.AnthropicCompleteChatStreamForStatisticsAsync(_httpContextAccessor.HttpContext,
                input,
                userId, null, tokenId, CancellationToken.None);
        }
        else
        {
            await _aiGateWayManager.AnthropicCompleteChatForStatisticsAsync(_httpContextAccessor.HttpContext, input,
                userId,
                null, tokenId,
                CancellationToken.None);
        }

        // 记录请求日志
        if (tokenValidation.IsEnableLog)
        {
            FireAndForgetMessageLog(JsonSerializer.Serialize(input), tokenValidation.Token, tokenValidation.TokenName, input.Model, ModelApiTypeEnum.Messages);
        }
    }


    /// <summary>
    /// 响应-Openai新规范
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1/responses")]
    public async Task ResponsesAsync([FromBody] OpenAiResponsesInput input, CancellationToken cancellationToken)
    {
        //前面都是校验，后面才是真正的调用
        var httpContext = this._httpContextAccessor.HttpContext;
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);



        //ai网关代理httpcontext
        if (input.Stream == true)
        {
            await _aiGateWayManager.OpenAiResponsesStreamForStatisticsAsync(_httpContextAccessor.HttpContext,
                input,
                userId, null, tokenId, CancellationToken.None);
        }
        else
        {
            await _aiGateWayManager.OpenAiResponsesAsyncForStatisticsAsync(_httpContextAccessor.HttpContext, input,
                userId,
                null, tokenId,
                CancellationToken.None);
        }

        // 记录请求日志
        if (tokenValidation.IsEnableLog)
        {
            FireAndForgetMessageLog(JsonSerializer.Serialize(input), tokenValidation.Token, tokenValidation.TokenName, input.Model, ModelApiTypeEnum.Responses);
        }
    }


    /// <summary>
    /// 生成-Gemini
    /// </summary>
    /// <param name="input"></param>
    /// <param name="modelId"></param>
    /// <param name="alt"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("openApi/v1beta/models/{modelId}:{action:regex(^(generateContent|streamGenerateContent)$)}")]
    public async Task GenerateContentAsync([FromBody] JsonElement input,
        [FromRoute] string modelId,
        [FromQuery] string? alt, CancellationToken cancellationToken)
    {
        //前面都是校验，后面才是真正的调用
        var httpContext = this._httpContextAccessor.HttpContext;
        var tokenValidation = await _tokenManager.ValidateTokenAsync(GetTokenByHttpContext(httpContext));
        var userId = tokenValidation.UserId;
        var tokenId = tokenValidation.TokenId;
        await _aiBlacklistManager.VerifiyAiBlacklist(userId);




        //ai网关代理httpcontext
        if (alt == "sse")
        {
            await _aiGateWayManager.GeminiGenerateContentStreamForStatisticsAsync(_httpContextAccessor.HttpContext,
                modelId, input,
                userId,
                null, tokenId,
                CancellationToken.None);
        }
        else
        {
            await _aiGateWayManager.GeminiGenerateContentForStatisticsAsync(_httpContextAccessor.HttpContext,
                modelId, input,
                userId,
                null, tokenId,
                CancellationToken.None);
        }

        // 记录请求日志
        if (tokenValidation.IsEnableLog)
        {
            FireAndForgetMessageLog(input.GetRawText(), tokenValidation.Token, tokenValidation.TokenName, modelId, ModelApiTypeEnum.GenerateContent);
        }
    }

    #region 私有

    private string? GetTokenByHttpContext(HttpContext httpContext)
    {
        // 优先从 x-api-key 获取
        string apiKeyHeader = httpContext.Request.Headers["x-api-key"];
        if (!string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return apiKeyHeader.Trim();
        }
        
        // 再从 谷歌 获取
        string googApiKeyHeader = httpContext.Request.Headers["x-goog-api-key"];
        if (!string.IsNullOrWhiteSpace(googApiKeyHeader))
        {
            return googApiKeyHeader.Trim();
        }

        // 再检查 Authorization 头
        string authHeader = httpContext.Request.Headers["Authorization"];
        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private void Intercept(HttpContext httpContext)
    {
        if (httpContext.Request.Host.Value == "yxai.chat")
        {
            throw new UserFriendlyException("当前海外站点不支持大流量接口，请使用转发站点：https://ai.ccnetcore.com");
        }
    }

    private void FireAndForgetMessageLog(string requestBody, string apiKey, string apiKeyName, string modelId, ModelApiTypeEnum apiType)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var manager = scope.ServiceProvider.GetRequiredService<MessageLogManager>();
                using var uow = uowManager.Begin(requiresNew: true);
                await manager.CreateAsync(requestBody, apiKey, apiKeyName, modelId, apiType);
                await uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录消息日志失败, 请求体长度: {RequestBodyLength}", requestBody?.Length ?? 0);
            }
        });
    }

    #endregion
}
