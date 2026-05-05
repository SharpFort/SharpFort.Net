using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.TenantManagement.Domain
{
    public interface ISqlSugarTenantRepository : ISqlSugarRepository<Tenant, Guid>
    {
        Task<Tenant> FindByNameAsync(string name, bool includeDetails = true);

        Task<List<Tenant>> GetListAsync(string? sorting = null,
            int maxResultCount = int.MaxValue,
            int skipCount = 0,
      string? filter = null,
      bool includeDetails = false);


        Task<long> GetCountAsync(
            string? filter = null);

    }
}
