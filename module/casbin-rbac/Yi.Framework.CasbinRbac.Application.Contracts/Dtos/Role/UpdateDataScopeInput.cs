using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class UpdateDataScopeInput
    {
        public Guid RoleId { get; set; }

        public List<Guid>? DepartmentIds { get; set; }

        public DataScope DataScope { get; set; }
    }
}
