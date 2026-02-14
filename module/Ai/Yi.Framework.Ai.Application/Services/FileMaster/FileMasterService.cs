using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.FileMaster;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Application.Services.FileMaster;

public class FileMasterService : ApplicationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AiGateWayManager _aiGateWayManager;
    private readonly AiBlacklistManager _aiBlacklistManager;

    public FileMasterService(IHttpContextAccessor httpContextAccessor, AiGateWayManager aiGateWayManager,
        AiBlacklistManager aiBlacklistManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _aiGateWayManager = aiGateWayManager;
        _aiBlacklistManager = aiBlacklistManager;
    }

    /// <summary>
    /// 校验下一步
    /// </summary>
    /// <returns></returns>
    [HttpPost("FileMaster/VerifyNext")]
    public Task<string> VerifyNextAsync(VerifyNextInput input)
    {
        if (!CurrentUser.IsAuthenticated)
        {
            if (input.DirectoryCount + input.FileCount >= 20)
            {
                throw new UserFriendlyException("未登录用户，文件夹与文件数量不能大于20个，请登录后解锁全部功能");
            }
        }
        else
        {
            if (input.DirectoryCount + input.FileCount >= 100)
            {
                throw new UserFriendlyException("为防止无限制暴力使用，当前文件整理大师Vip最多支持100文件与文件夹数量");
            }
        }

        return Task.FromResult("success");
    }


    /// <summary>
    /// 对话
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    [HttpPost("FileMaster/chat/completions")]
    public async Task ChatCompletionsAsync([FromBody] ThorChatCompletionsRequest input,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.IsAuthenticated)
        {
            input.Model = "gpt-5-chat";
        }
        else
        {
            input.Model = "gpt-5-chat";
        }

        Guid? userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : null;
        if (userId is not null)
        {
            await _aiBlacklistManager.VerifiyAiBlacklist(userId.Value);
        }

        //ai网关代理httpcontext
        if (input.Stream == true)
        {
            await _aiGateWayManager.CompleteChatStreamForStatisticsAsync(_httpContextAccessor.HttpContext, input,
                userId, null, null, cancellationToken);
        }
        else
        {
            await _aiGateWayManager.CompleteChatForStatisticsAsync(_httpContextAccessor.HttpContext, input, userId,
                null, null,
                cancellationToken);
        }
    }
}
