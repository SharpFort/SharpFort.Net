using Volo.Abp.Application.Dtos;
using Yi.Framework.Rbac.Application.Contracts.Dtos.Dept;
using Yi.Framework.Rbac.Application.Contracts.Dtos.Post;
using Yi.Framework.Rbac.Application.Contracts.Dtos.Role;
using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Application.Contracts.Dtos.User
{
    public class UserGetOutputDto : EntityDto<Guid>
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Nick { get; set; }
        public string? Email { get; set; }
        public string? Ip { get; set; }
        public string? Address { get; set; }
        public long? Phone { get; set; }
        public string? Introduction { get; set; }
        public string? Remark { get; set; }
        public Gender Gender { get; set; } = Gender.Unknown;
        public bool State { get; set; }
        public DateTime CreationTime { get; set; }

        public Guid? DepartmentId { get; set; }

        public DeptGetOutputDto? Dept { get; set; }

        public List<PostGetListOutputDto>? Posts { get; set; }

        public List<RoleGetListOutputDto>? Roles { get; set; }
    }
}
