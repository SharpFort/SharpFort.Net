// IDE0130: ABP 框架覆盖, 必须使用 Volo.Abp 命名空间才能被 ABP DI 扫描替换
#pragma warning disable IDE0130
using SharpFort.SettingManagement.Domain;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;

namespace Volo.Abp.SettingManagement.EntityFrameworkCore;

#pragma warning disable CS8767 // ABP IRepository 接口 setter 参数可空性不匹配（继承自基类）
public class SqlSugarCoreSettingRepository(ISugarDbContextProvider<ISqlSugarDbContext> sugarDbContextProvider) : SqlSugarRepository<SettingAggregateRoot, Guid>(sugarDbContextProvider),
    ISettingRepository
{
    public virtual async Task<SettingAggregateRoot> FindAsync(
        string name,
        string providerName,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        return await _DbQueryable
            .Where(s => s.Name == name && s.ProviderName == providerName && s.ProviderKey == providerKey)
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    public virtual async Task<List<SettingAggregateRoot>> GetListAsync(
        string providerName,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        return await _DbQueryable
            .Where(
                s => s.ProviderName == providerName && s.ProviderKey == providerKey
            ).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<SettingAggregateRoot>> GetListAsync(
        string[] names,
        string providerName,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        return await _DbQueryable
            .Where(
                s => names.Contains(s.Name) && s.ProviderName == providerName && s.ProviderKey == providerKey
            ).ToListAsync(cancellationToken);
    }
}
