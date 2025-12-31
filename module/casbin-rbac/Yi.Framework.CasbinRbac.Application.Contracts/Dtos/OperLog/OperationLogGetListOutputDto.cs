using Volo.Abp.Application.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.OperLog
{
    public class OperationLogGetListOutputDto : EntityDto<Guid>
    {
        public string? Title { get; set; }
        public OperationType OperationType { get; set; }
        public string? RequestMethod { get; set; }
        public string? OperUser { get; set; }
        public string? OperIp { get; set; }
        public string? OperLocation { get; set; }
        public string? Method { get; set; }
        public string? RequestParam { get; set; }
        public string? RequestResult { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
