using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Menu
{
    public class MenuUpdateInputVo 
    {
        public Guid Id { get; set; }
        public Guid? CreatorId { get; set; }
        public bool State { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public MenuType MenuType { get; set; } = MenuType.Menu;
        public string? PermissionCode { get; set; }
        public Guid ParentId { get; set; }
        public string? MenuIcon { get; set; }
        public string? Router { get; set; }
        public bool IsLink { get; set; }
        public bool IsCache { get; set; }
        public bool IsShow { get; set; } = true;
        public string? Remark { get; set; }
        public string? Component { get; set; }
        public string? Query { get; set; }
        public int OrderNum { get; set; }
        public string? RouterName { get; set; }
        public string? ApiUrl { get; set; }
        public string? ApiMethod { get; set; }
    }
}
