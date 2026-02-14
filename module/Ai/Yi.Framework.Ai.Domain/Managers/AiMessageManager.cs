using Volo.Abp.Domain.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

public class AiMessageManager : DomainService
{
    private readonly ISqlSugarRepository<ChatMessage> _repository;

    public AiMessageManager(ISqlSugarRepository<ChatMessage> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 创建系统消息
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="sessionId">会话Id</param>
    /// <param name="input">消息输入</param>
    /// <param name="tokenId">Token Id（Web端传Guid.Empty）</param>
    /// <returns></returns>
    public async Task<Guid> CreateSystemMessageAsync(Guid? userId, Guid? sessionId, MessageInputDto input, Guid? tokenId = null)
    {
        input.Role = "system";
        var message = new ChatMessage(userId, sessionId, input.Content, input.Role, input.ModelId, input.TokenUsage, tokenId);
        await _repository.InsertAsync(message);
        return message.Id;
    }

    /// <summary>
    /// 创建用户消息
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="sessionId">会话Id</param>
    /// <param name="input">消息输入</param>
    /// <param name="tokenId">Token Id（Web端传Guid.Empty）</param>
    /// <param name="createTime"></param>
    /// <returns></returns>
    public async Task<Guid> CreateUserMessageAsync( Guid? userId, Guid? sessionId, MessageInputDto input, Guid? tokenId = null,DateTime? createTime=null)
    {
        input.Role = "user";
        var message = new ChatMessage(userId, sessionId, input.Content, input.Role, input.ModelId, input.TokenUsage, tokenId)
        {
            CreationTime = createTime??DateTime.Now
        };
        await _repository.InsertAsync(message);
        return message.Id;
    }
}
