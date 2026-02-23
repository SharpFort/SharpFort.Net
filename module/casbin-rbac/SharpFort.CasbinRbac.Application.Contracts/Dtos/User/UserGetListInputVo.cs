using SharpFort.Ddd;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.User
{
    public class UserGetListInputVo : PagedAllResultRequestDto
    {
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public long? Phone { get; set; }

        public bool? State { get; set; }

        public Guid? DepartmentId { get; set; }

        public string? Ids { get; set; }
    }
}
