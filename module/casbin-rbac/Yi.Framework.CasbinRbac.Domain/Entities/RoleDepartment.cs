using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 角色-部门关联实体
/// 用于存储“自定义数据权限”的具体部门范围
/// </summary>
[SugarTable("casbin_sys_role_Department")] // 建议缩写表名，保持简洁
// 核心约束：同一个角色不能重复绑定同一个部门，设置联合唯一索引
[SugarIndex("index_role_Department_unique", nameof(RoleId),OrderByType.Asc, nameof(DepartmentId),OrderByType.Asc, IsUnique = true)]
public class RoleDepartment : CreationAuditedEntity<Guid>
{
    #region 构造函数

    /// <summary>
    /// ORM 专用
    /// </summary>
    public RoleDepartment() { }

    /// <summary>
    /// 创建关联
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="DepartmentId">部门ID</param>
    public RoleDepartment(Guid roleId, Guid DepartmentId)
    {
        RoleId = roleId;
        DepartmentId = DepartmentId;
    }

    /// <summary>
    /// 构造函数重载（带主键生成策略，如果需要手动指定Id）
    /// </summary>
    public RoleDepartment(Guid id, Guid roleId, Guid DepartmentId) : base(id)
    {
        RoleId = roleId;
        DepartmentId = DepartmentId;
    }

    #endregion

    #region 核心属性

    ///// <summary>
    ///// 主键
    ///// </summary>
    //[SugarColumn(IsPrimaryKey = true)]
    //public override Guid Id { get; protected set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get;  set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public Guid DepartmentId { get;  set; }

    #endregion

    #region 导航属性 (仅供查询)

    // 即使是中间表，加上导航属性也可以方便进行“Include/Join”查询
    // 例如：查询某个关联记录时，顺便带出部门名称

    /// <summary>
    /// 关联的角色
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public Role? Role { get; set; }

    /// <summary>
    /// 关联的部门
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(DepartmentId))]
    // 假设您的部门实体类名为 Department 或 Department
    public Department? Department { get; set; }

    #endregion
}

