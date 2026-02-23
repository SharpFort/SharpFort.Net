using Volo.Abp.Application.Services;
using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Dept;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// Dept服务抽象
    /// </summary>
    public interface IDeptService : ISfCrudAppService<DeptGetOutputDto, DeptGetListOutputDto, Guid, DeptGetListInputVo, DeptCreateInputVo, DeptUpdateInputVo>
    {
        Task<List<Guid>> GetChildListAsync(Guid deptId);
    }
}
