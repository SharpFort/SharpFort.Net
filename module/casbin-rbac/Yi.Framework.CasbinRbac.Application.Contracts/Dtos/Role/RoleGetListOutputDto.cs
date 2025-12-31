using Volo.Abp.Application.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class RoleGetListOutputDto : EntityDto<Guid>
    {
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public Guid? CreatorId { get; set; }
        public string? RoleName { get; set; }
        public string? RoleCode { get; set; }
        public string? Remark { get; set; }
        public DataScope DataScope { get; set; } = DataScope.ALL;
        public bool State { get; set; }

        public int OrderNum { get; set; }
    }
}
