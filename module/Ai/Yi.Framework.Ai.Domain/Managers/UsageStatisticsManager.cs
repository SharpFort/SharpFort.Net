using Medallion.Threading;
using Volo.Abp.Domain.Services;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Domain.Managers;

public class UsageStatisticsManager : DomainService
{
    private readonly ISqlSugarRepository<AiUsage> _repository;

    public UsageStatisticsManager(ISqlSugarRepository<AiUsage> repository)
    {
        _repository = repository;
    }

    private IDistributedLockProvider DistributedLock =>
        LazyServiceProvider.LazyGetRequiredService<IDistributedLockProvider>();

    public async Task SetUsageAsync(Guid? userId, string modelId, ThorUsageResponse? tokenUsage, Guid? tokenId = null)
    {
        var actualTokenId = tokenId ?? Guid.Empty;

        long inputTokenCount = tokenUsage?.PromptTokens > 0
            ? tokenUsage.PromptTokens.Value
            : tokenUsage?.InputTokens ?? 0;

        long outputTokenCount = tokenUsage?.CompletionTokens > 0
            ? tokenUsage.CompletionTokens.Value
            : tokenUsage?.OutputTokens ?? 0;

        await using (await DistributedLock.AcquireLockAsync($"UsageStatistics:{userId?.ToString()}:{actualTokenId}:{modelId}"))
        {
            var entity = await _repository._DbQueryable.FirstAsync(x => x.UserId == userId && x.ModelId == modelId && x.TokenId == actualTokenId);
            //存在数据，更新
            if (entity is not null)
            {
                entity.AddOnceChat(inputTokenCount, outputTokenCount);
                await _repository.UpdateAsync(entity);
            }
            //不存在插入
            else
            {
                var usage = new AiUsage(userId, modelId, actualTokenId);
                usage.AddOnceChat(inputTokenCount, outputTokenCount);
                await _repository.InsertAsync(usage);
            }
        }
    }
}
