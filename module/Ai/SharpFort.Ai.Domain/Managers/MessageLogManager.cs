using Volo.Abp.Domain.Services;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Domain.Managers;

[Obsolete("Use MessageLogRepository or similar replacement instead.")]
public class MessageLogManager(ISqlSugarRepository<MessageLogAggregateRoot> repository) : DomainService
{
    [Obsolete("Use MessageLogRepository or similar replacement instead.")]
    private readonly ISqlSugarRepository<MessageLogAggregateRoot> _repository = repository;

    /// <summary>
    /// 创建消息日志
    /// </summary>
    [Obsolete("Use MessageLogRepository or similar replacement instead.")]
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
