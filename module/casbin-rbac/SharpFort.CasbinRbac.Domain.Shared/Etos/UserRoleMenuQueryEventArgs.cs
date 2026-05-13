using SharpFort.CasbinRbac.Domain.Shared.Dtos;

namespace SharpFort.CasbinRbac.Domain.Shared.Etos
{
    public class UserRoleMenuQueryEventArgs
    {
        public UserRoleMenuQueryEventArgs() { }

        public UserRoleMenuQueryEventArgs(params Guid[] userIds)
        {
            UserIds.AddRange([.. userIds]);
        }
        public List<Guid> UserIds { get; set; } = [];

        public List<UserRoleMenuDto>? Result { get; set; }
    }
}
