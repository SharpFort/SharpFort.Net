using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Menu
{
    /// <summary>
    /// Menu输入创建对象
    /// 系统字段 Id/CreationTime/CreatorId 由 ABP 审计自动填充
    /// MenuSource 由实体构造函数默认赋值
    /// </summary>
    public class MenuCreateInputVo
    {
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
