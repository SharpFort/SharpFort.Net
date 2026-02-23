using SqlSugar;
using Volo.Abp.DependencyInjection;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Repositories;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;

namespace SharpFort.CasbinRbac.SqlSugarCore.Repositories
{
    public class DeptRepository : SqlSugarRepository<Department, Guid>, IDeptRepository, ITransientDependency
    {
        public DeptRepository(ISugarDbContextProvider<ISqlSugarDbContext> sugarDbContextProvider) : base(sugarDbContextProvider)
        {
        }

        public async Task<List<Guid>> GetChildListAsync(Guid deptId)
        {
            var entities = await _DbQueryable.ToChildListAsync(x => x.ParentId, deptId);
            return entities.Select(x => x.Id).ToList();
        }
        public async Task<List<Department>> GetListRoleIdAsync(Guid roleId)
        {

            return await _DbQueryable.Where(d => SqlFunc.Subqueryable<RoleDepartment>().Where(rd => rd.RoleId == roleId && d.Id == rd.DepartmentId).Any()).ToListAsync();
        }
    }
}
