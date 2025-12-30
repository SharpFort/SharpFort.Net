using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.Role
{
    /// <summary>
    /// Role输入创建对象
    /// </summary>
    public class RoleCreateInputVo
    {
        public string? RoleName { get; set; }
        public string? RoleCode { get; set; }
        public string? Remark { get; set; }
        public DataScope DataScope { get; set; } = DataScope.ALL;
        public bool State { get; set; } = true;

        public int OrderNum { get; set; }

        public List<Guid> DepartmentIds { get; set; }

        public List<Guid> MenuIds { get; set; }
    }
}
