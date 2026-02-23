using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.OperLog
{
    public class OperationLogGetListInputVo : PagedAllResultRequestDto
    {
        public OperationType? OperationType { get; set; }
        public string? OperUser { get; set; }
    }
}
