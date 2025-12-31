using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.OperLog;

namespace Yi.Framework.CasbinRbac.Application.Contracts.IServices
{
    /// <summary>
    /// OperationLog服务抽象
    /// </summary>
    public interface IOperationLogService : IYiCrudAppService<OperationLogGetListOutputDto, Guid, OperationLogGetListInputVo>
    {

    }
}
