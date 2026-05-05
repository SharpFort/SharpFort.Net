using SharpFort.SettingManagement.Domain;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;

namespace Volo.Abp.SettingManagement.EntityFrameworkCore;

#pragma warning disable CS8767 // ABP IRepository 接口 setter 参数可空性不匹配（继承自基类）
public class SqlSugarCoreSettingRepository : SqlSugarRepository<SettingAggregateRoot, Guid>,
    ISettingRepository
{
    public SqlSugarCoreSettingRepository(ISugarDbContextProvider<ISqlSugarDbContext> sugarDbContextProvider) : base(sugarDbContextProvider)
    {
    }

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
