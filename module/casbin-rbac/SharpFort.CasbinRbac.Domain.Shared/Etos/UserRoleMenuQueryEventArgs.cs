using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;

namespace SharpFort.CasbinRbac.Domain.Shared.Etos
{
    public class UserRoleMenuQueryEventArgs
    {
        public UserRoleMenuQueryEventArgs() { }

        public UserRoleMenuQueryEventArgs(params Guid[] userIds)
        {
            UserIds.AddRange(userIds.ToList());
        }
        public List<Guid> UserIds { get; set; } = new List<Guid>();

        public List<UserRoleMenuDto>? Result { get; set; }
    }
}
