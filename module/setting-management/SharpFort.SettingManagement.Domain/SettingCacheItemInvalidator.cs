using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;

namespace SharpFort.SettingManagement.Domain;

public class SettingCacheItemInvalidator(IDistributedCache<SettingCacheItem> cache) : ILocalEventHandler<EntityChangedEventData<SettingAggregateRoot>>, ITransientDependency
{
    protected IDistributedCache<SettingCacheItem> Cache { get; } = cache;

    public virtual async Task HandleEventAsync(EntityChangedEventData<SettingAggregateRoot> eventData)
    {
        var cacheKey = CalculateCacheKey(
            eventData.Entity.Name,
            eventData.Entity.ProviderName!,
            eventData.Entity.ProviderKey!
        );

        await Cache.RemoveAsync(cacheKey, considerUow: true);
    }

    protected virtual string CalculateCacheKey(string name, string providerName, string providerKey)
    {
        return SettingCacheItem.CalculateCacheKey(name, providerName, providerKey);
    }
}
