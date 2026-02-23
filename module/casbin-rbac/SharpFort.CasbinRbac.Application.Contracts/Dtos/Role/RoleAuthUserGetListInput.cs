using Volo.Abp.Application.Dtos;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class RoleAuthUserGetListInput : PagedAndSortedResultRequestDto
    {
        public string? UserName { get; set; }

        public long? Phone { get; set; }
    }
}
