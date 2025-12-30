using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.Role
{
    public class RoleUpdateInputVo
    {
        public string? RoleName { get; set; }
        public string? RoleCode { get; set; }
        public string? Remark { get; set; }
        public DataScope DataScope { get; set; } = DataScope.ALL;
        public bool State { get; set; }

        public int OrderNum { get; set; }

        public List<Guid>? DepartmentIds { get; set; }

        public List<Guid>? MenuIds { get; set; }
    }
}
