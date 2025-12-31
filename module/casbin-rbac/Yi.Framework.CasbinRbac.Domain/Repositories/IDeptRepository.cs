using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Repositories
{
    public interface IDeptRepository : ISqlSugarRepository<Department, Guid>
    {
        Task<List<Guid>> GetChildListAsync(Guid deptId);
        Task<List<Department>> GetListRoleIdAsync(Guid roleId);
    }
}
