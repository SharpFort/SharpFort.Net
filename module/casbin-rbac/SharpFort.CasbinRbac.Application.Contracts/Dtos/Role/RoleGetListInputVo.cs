using SharpFort.Ddd;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class RoleGetListInputVo : PagedAllResultRequestDto
    {
        public string? RoleName { get; set; }
        public string? RoleCode { get; set; }
        public bool? State { get; set; }


    }
}
