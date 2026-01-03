using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 角色-菜单关联实体
/// 用于存储角色拥有的菜单/权限
/// </summary>
[SugarTable("casbin_sys_role_menu")]
// 核心约束：防止重复授权，建立联合唯一索引
[SugarIndex("index_role_menu_unique", nameof(RoleId), OrderByType.Asc, nameof(MenuId), OrderByType.Asc, IsUnique = true)]
public class RoleMenu : CreationAuditedEntity<Guid>
{
    #region 构造函数

    /// <summary>
    /// ORM 专用无参构造函数
    /// </summary>
    public RoleMenu() { }

    /// <summary>
    /// 创建关联
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuId">菜单ID</param>
    public RoleMenu(Guid roleId, Guid menuId)
    {
        RoleId = roleId;
        MenuId = menuId;
    }

    /// <summary>
    /// 构造函数重载 (带ID)
    /// </summary>
    public RoleMenu(Guid id, Guid roleId, Guid menuId) : base(id)
    {
        RoleId = roleId;
        MenuId = menuId;
    }

    #endregion

    #region 核心属性

    /// <summary>
    /// 主键
    /// </summary>
    //[SugarColumn(IsPrimaryKey = true)]
    //public override Guid Id { get;  set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get;  set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public Guid MenuId { get;  set; }

    #endregion

    #region 导航属性 (仅供查询)

    /// <summary>
    /// 关联的角色
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public Role? Role { get; set; }

    /// <summary>
    /// 关联的菜单
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(MenuId))]
    public Menu? Menu { get; set; }

    #endregion
}
