using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.Role
{
    public class UpdateDataScopeInput
    {
        public Guid RoleId { get; set; }

        public List<Guid>? DepartmentIds { get; set; }

        public DataScope DataScope { get; set; }
    }
}
