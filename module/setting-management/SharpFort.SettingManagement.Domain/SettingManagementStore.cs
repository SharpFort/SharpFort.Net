using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace SharpFort.SettingManagement.Domain;

public class SettingManagementStore(
    ISettingRepository settingRepository,
    IGuidGenerator guidGenerator,
    IDistributedCache<SettingCacheItem> cache,
    ISettingDefinitionManager settingDefinitionManager) : ISettingManagementStore, ITransientDependency
{
    protected IDistributedCache<SettingCacheItem> Cache { get; } = cache;
    protected ISettingDefinitionManager SettingDefinitionManager { get; } = settingDefinitionManager;
    protected ISettingRepository SettingRepository { get; } = settingRepository;
    protected IGuidGenerator GuidGenerator { get; } = guidGenerator;

    [UnitOfWork]
    public virtual async Task<string> GetOrNullAsync(string name, string providerName, string providerKey)
    {
        return (await GetCacheItemAsync(name, providerName, providerKey)).Value!;
    }

    [UnitOfWork]
    public virtual async Task SetAsync(string name, string value, string providerName, string providerKey)
    {
        SettingAggregateRoot setting = await SettingRepository.FindAsync(name, providerName, providerKey);
        if (setting == null)
        {
            setting = new SettingAggregateRoot(GuidGenerator.Create(), name, value, providerName, providerKey);
            await SettingRepository.InsertAsync(setting);
        }
        else
        {
            setting.Value = value;
            await SettingRepository.UpdateAsync(setting);
        }

        await Cache.SetAsync(CalculateCacheKey(name, providerName, providerKey), new SettingCacheItem(setting.Value), considerUow: true);
    }

    public virtual async Task<List<SettingValue>> GetListAsync(string providerName, string providerKey)
    {
        List<SettingAggregateRoot> settings = await SettingRepository.GetListAsync(providerName, providerKey);
        return [.. settings.Select(s => new SettingValue(s.Name, s.Value))];
    }

    [UnitOfWork]
    public virtual async Task DeleteAsync(string name, string providerName, string providerKey)
    {
        SettingAggregateRoot setting = await SettingRepository.FindAsync(name, providerName, providerKey);
        if (setting != null)
        {
            await SettingRepository.DeleteAsync(setting);
            await Cache.RemoveAsync(CalculateCacheKey(name, providerName, providerKey), considerUow: true);
        }
    }

    protected virtual async Task<SettingCacheItem> GetCacheItemAsync(string name, string providerName, string providerKey)
    {
        string cacheKey = CalculateCacheKey(name, providerName, providerKey);
        SettingCacheItem? cacheItem = await Cache.GetAsync(cacheKey, considerUow: true);

        if (cacheItem != null)
        {
            return cacheItem;
        }

        cacheItem = new SettingCacheItem(null);

        await SetCacheItemsAsync(providerName, providerKey, name, cacheItem);

        return cacheItem;
    }

    private async Task SetCacheItemsAsync(
        string providerName,
        string providerKey,
        string currentName,
        SettingCacheItem currentCacheItem)
    {
        IReadOnlyList<SettingDefinition> settingDefinitions = await SettingDefinitionManager.GetAllAsync();
        Dictionary<string, string> settingsDictionary = (await SettingRepository.GetListAsync(providerName, providerKey))
            .ToDictionary(s => s.Name, s => s.Value);

        List<KeyValuePair<string, SettingCacheItem>> cacheItems = [];

        foreach (SettingDefinition settingDefinition in settingDefinitions)
        {
            string? settingValue = settingsDictionary.GetOrDefault(settingDefinition.Name);

            cacheItems.Add(
                new KeyValuePair<string, SettingCacheItem>(
                    CalculateCacheKey(settingDefinition.Name, providerName, providerKey),
                    new SettingCacheItem(settingValue)
                )
            );

            if (settingDefinition.Name == currentName)
            {
                currentCacheItem.Value = settingValue;
            }
        }

        await Cache.SetManyAsync(cacheItems, considerUow: true);
    }

    [UnitOfWork]
    public async Task<List<SettingValue>> GetListAsync(string[] names, string providerName, string providerKey)
    {
        Check.NotNullOrEmpty(names, nameof(names));

        List<SettingValue> result = [];

        if (names.Length == 1)
        {
            string name = names.First();
            result.Add(new SettingValue(name, (await GetCacheItemAsync(name, providerName, providerKey)).Value));
            return result;
        }

        List<KeyValuePair<string, SettingCacheItem>> cacheItems = await GetCacheItemsAsync(names, providerName, providerKey);
        foreach (KeyValuePair<string, SettingCacheItem> item in cacheItems)
        {
            result.Add(new SettingValue(GetSettingNameFormCacheKeyOrNull(item.Key), item.Value?.Value));
        }

        return result;
    }

    protected virtual async Task<List<KeyValuePair<string, SettingCacheItem>>> GetCacheItemsAsync(string[] names, string providerName, string providerKey)
    {
        List<string> cacheKeys = [.. names.Select(x => CalculateCacheKey(x, providerName, providerKey))];

        List<KeyValuePair<string, SettingCacheItem?>> cacheItems = [.. (await Cache.GetManyAsync(cacheKeys, considerUow: true))];

        if (cacheItems.All(x => x.Value != null))
        {
            return [.. cacheItems.Select(kvp => new KeyValuePair<string, SettingCacheItem>(kvp.Key, kvp.Value!))];
        }

        List<string> notCacheKeys = [.. cacheItems.Where(x => x.Value == null).Select(x => x.Key)];

        List<KeyValuePair<string, SettingCacheItem>> newCacheItems = await SetCacheItemsAsync(providerName, providerKey, notCacheKeys);

        List<KeyValuePair<string, SettingCacheItem>> result = [];
        foreach (string? key in cacheKeys)
        {
            KeyValuePair<string, SettingCacheItem> item = newCacheItems.FirstOrDefault(x => x.Key == key);
            if (item.Value == null)
            {
#pragma warning disable CS8619 // cacheItems FirstOrDefault 类型可空性不匹配
                item = cacheItems.FirstOrDefault(x => x.Key == key);
#pragma warning restore CS8619
            }

            result.Add(new KeyValuePair<string, SettingCacheItem>(key, item.Value!));
        }

        return result;
    }

    private async Task<List<KeyValuePair<string, SettingCacheItem>>> SetCacheItemsAsync(
        string providerName,
        string providerKey,
        List<string> notCacheKeys)
    {
        IEnumerable<SettingDefinition> settingDefinitions = (await SettingDefinitionManager.GetAllAsync()).Where(x => notCacheKeys.Any(k => GetSettingNameFormCacheKeyOrNull(k) == x.Name));

        Dictionary<string, string> settingsDictionary = (await SettingRepository.GetListAsync([.. notCacheKeys.Select(GetSettingNameFormCacheKeyOrNull)], providerName, providerKey))
            .ToDictionary(s => s.Name, s => s.Value);

        List<KeyValuePair<string, SettingCacheItem>> cacheItems = [];

        foreach (SettingDefinition? settingDefinition in settingDefinitions)
        {
            string? settingValue = settingsDictionary.GetOrDefault(settingDefinition.Name);
            cacheItems.Add(
                new KeyValuePair<string, SettingCacheItem>(
                    CalculateCacheKey(settingDefinition.Name, providerName, providerKey),
                    new SettingCacheItem(settingValue)
                )
            );
        }

        await Cache.SetManyAsync(cacheItems, considerUow: true);

        return cacheItems;
    }


    protected virtual string CalculateCacheKey(string name, string providerName, string providerKey)
    {
        return SettingCacheItem.CalculateCacheKey(name, providerName, providerKey);
    }

    protected virtual string GetSettingNameFormCacheKeyOrNull(string key)
    {
        //TODO: throw ex when name is null?
        return SettingCacheItem.GetSettingNameFormCacheKeyOrNull(key)!;
    }
}
