using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.OperLog
{
    public class OperationLogGetListInputVo : PagedAllResultRequestDto
    {
        public OperationType? OperationType { get; set; }
        public string? OperUser { get; set; }
    }
}
