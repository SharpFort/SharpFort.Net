using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Repositories
{
    public interface IDeptRepository : ISqlSugarRepository<Department, Guid>
    {
        Task<List<Guid>> GetChildListAsync(Guid deptId);
        Task<List<Department>> GetListRoleIdAsync(Guid roleId);
    }
}
