using System.Text.Json;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.Dtos.AiModel;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Contracts.IServices;

/// <summary>
/// AI聊天服务接口
/// </summary>
public interface IAiChatService : IApplicationService
{
    /// <summary>
    /// 获取可用的对话模型列表
    /// </summary>
    Task<List<AiModelDto>> GetModelListAsync();

    /// <summary>
    /// 统一发送消息 - 支持多种API类型
    /// </summary>
    /// <param name="apiType">API类型枚举</param>
    /// <param name="input">原始请求体JsonElement</param>
    /// <param name="modelId">模型ID</param>
    /// <param name="sessionId">会话ID</param>
    Task UnifiedSendAsync(ModelApiTypeEnum apiType, JsonElement input, string modelId, Guid? sessionId);
}
