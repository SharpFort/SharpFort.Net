using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

public class MessageLogManager(ISqlSugarRepository<MessageLogAggregateRoot> repository) : DomainService
{
    private readonly ISqlSugarRepository<MessageLogAggregateRoot> _repository = repository;

    /// <summary>
    /// 创建消息日志
    /// </summary>
    public async Task CreateAsync(string requestBody, string apiKey, string apiKeyName, string modelId, ModelApiType apiType)
    {
        MessageLogAggregateRoot entity = new()
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
