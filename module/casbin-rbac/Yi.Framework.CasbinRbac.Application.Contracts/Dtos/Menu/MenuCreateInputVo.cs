using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Menu
{
    /// <summary>
    /// Menu输入创建对象
    /// </summary>
    public class MenuCreateInputVo 
    {
        public Guid? Id { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.Now;
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
        public MenuSource MenuSource { get; set; } = MenuSource.Ruoyi;
        public string? RouterName { get; set; }
        
        /// <summary>
        /// API URL (用于 Casbin 鉴权，例如 /api/user)
        /// </summary>
        public string? ApiUrl { get; set; }

        /// <summary>
        /// API Method (用于 Casbin 鉴权，例如 GET, POST)
        /// </summary>
        public string? ApiMethod { get; set; }
    }
}
