using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using SharpFort.Ai.Application.Contracts.IServices;

namespace SharpFort.Ai.Application.Services;

/// <summary>
/// AI工具服务
/// </summary>
[Authorize]
public class AiToolService(AiChatService chatService) : ApplicationService, IAiToolService
{
    private readonly AiChatService _chatService = chatService;

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
