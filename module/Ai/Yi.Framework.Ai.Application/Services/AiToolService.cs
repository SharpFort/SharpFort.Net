using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Managers;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// AI工具服务
/// </summary>
[Authorize]
public class AiToolService : ApplicationService, IAiToolService
{
    private readonly AiChatService _chatService;
    // Assume we use ChatService for implementing tools or direct gateway access

    public AiToolService(AiChatService chatService)
    {
        _chatService = chatService;
    }

    public Task<string> TranslateAsync(string text, string targetLang, string? modelId = null)
    {
        throw new NotImplementedException();
    }

    public Task<string> SummarizeAsync(string content, string? modelId = null)
    {
        throw new NotImplementedException();
    }

    public Task<string> SearchAsync(string query, string? modelId = null)
    {
        throw new NotImplementedException();
    }
}
