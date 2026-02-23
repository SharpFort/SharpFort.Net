using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.OperLog;

namespace SharpFort.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// OperationLog服务抽象
    /// </summary>
    public interface IOperationLogService : ISfCrudAppService<OperationLogGetListOutputDto, Guid, OperationLogGetListInputVo>
    {

    }
}
