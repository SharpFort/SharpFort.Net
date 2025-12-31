using System.ComponentModel.DataAnnotations;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Role
{
    public class RoleAuthUserCreateOrDeleteInput
    {
        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public List<Guid> UserIds { get; set; }
    }
}
