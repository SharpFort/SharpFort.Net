using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

public class MessageLogManager : DomainService
{
    private readonly ISqlSugarRepository<MessageLogAggregateRoot> _repository;

    public MessageLogManager(ISqlSugarRepository<MessageLogAggregateRoot> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 创建消息日志
    /// </summary>
    public async Task CreateAsync(string requestBody, string apiKey, string apiKeyName, string modelId, ModelApiTypeEnum apiType)
    {
        var entity = new MessageLogAggregateRoot
        {
            RequestBody = requestBody,
            ApiKey = apiKey,
            ApiKeyName = apiKeyName,
            ModelId = modelId,
            ApiType = apiType,
            ApiTypeName = apiType.ToString(),
            CreationTime = DateTime.Now
        };
        await _repository.InsertAsync(entity);
    }
}
