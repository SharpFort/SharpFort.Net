using Yi.Framework.Rbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Rbac.Domain.Repositories
{
    public interface IDeptRepository : ISqlSugarRepository<Department, Guid>
    {
        Task<List<Guid>> GetChildListAsync(Guid deptId);
        Task<List<Department>> GetListRoleIdAsync(Guid roleId);
    }
}
