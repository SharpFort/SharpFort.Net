using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.OperLog
{
    public class OperationLogGetListInputVo : PagedAllResultRequestDto
    {
        public OperationType? OperationType { get; set; }
        public string? OperUser { get; set; }
    }
}
