using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 用户-角色关联实体
/// 用于存储用户拥有的角色信息
/// </summary>
[SugarTable("casbin_sys_user_role")]
// 核心约束：防止重复授权，建立联合唯一索引
[SugarIndex("index_user_role_unique", nameof(UserId),OrderByType.Asc, nameof(RoleId), OrderByType.Asc, IsUnique = true)]
public class UserRole : CreationAuditedEntity<Guid>
{
    #region 构造函数

    /// <summary>
    /// ORM 专用无参构造函数
    /// </summary>
    public UserRole() { }

    /// <summary>
    /// 创建关联
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleId">角色ID</param>
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    /// <summary>
    /// 构造函数重载 (带ID)
    /// </summary>
    public UserRole(Guid id, Guid userId, Guid roleId) : base(id)
    {
        UserId = userId;
        RoleId = roleId;
    }

    #endregion

    #region 核心属性

    ///// <summary>
    ///// 主键
    ///// </summary>
    //[SugarColumn(IsPrimaryKey = true)]
    //public override Guid Id { get; protected set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get;  set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get;  set; }

    #endregion

    #region 导航属性 (仅供查询)

    /// <summary>
    /// 关联的用户
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 关联的角色
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public Role? Role { get; set; }

    #endregion
}
