using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class UpdateDataScopeInput
    {
        public Guid RoleId { get; set; }

        public List<Guid>? DepartmentIds { get; set; }

        public DataScope DataScope { get; set; }
    }
}
