using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.Core.Data;
using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 角色聚合根
/// 系统核心权限控制实体，使用 FullAuditedAggregateRoot 确保全生命周期可追溯
/// </summary>
[SugarTable("sys_role")]
// 角色编码必须唯一，防止鉴权冲突
[SugarIndex($"index_{nameof(RoleCode)}", nameof(RoleCode), OrderByType.Asc, IsUnique = true)]
public class Role : FullAuditedAggregateRoot<Guid>, IOrderNum, IState, IMultiTenant
{
    #region 构造函数

    /// <summary>
    /// ORM 专用无参构造函数
    /// </summary>
    public Role() { }

    /// <summary>
    /// 创建新角色
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <param name="roleName">角色名称</param>
    /// <param name="roleCode">角色编码</param>
    /// <param name="dataScope">数据范围</param>
    public Role(Guid id, string roleName, string roleCode, DataScope dataScope = DataScope.SELF)
        : base(id)
    {
        Volo.Abp.Check.NotNullOrWhiteSpace(roleName, nameof(roleName));
        Volo.Abp.Check.NotNullOrWhiteSpace(roleCode, nameof(roleCode));

        RoleName = roleName;
        RoleCode = roleCode;
        DataScope = dataScope;

        // 默认值
        State = true;
        OrderNum = 0;
    }

    #endregion

    #region 核心属性

    /// <summary>
    /// 主键
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public override Guid Id { get; protected set; }
    
    /// <summary>
    /// 租户ID
    /// </summary>
    public Guid? TenantId { get; protected set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    [SugarColumn(Length = 64)]
    public string RoleName { get; protected set; }

    /// <summary>
    /// 角色编码
    /// 核心业务标识，建议仅允许创建时设置，或通过特定更名方法修改
    /// </summary>
    [SugarColumn(Length = 64)]
    public string RoleCode { get; protected set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    /// <summary>
    /// 数据范围
    /// </summary>
    public DataScope DataScope { get; set; }

    /// <summary>
    /// 排序 (IOrderNum 实现)
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 状态 (IState 实现)
    /// </summary>
    public bool State { get; set; }

    #endregion

    #region 导航属性

    /// <summary>
    /// 角色-菜单 关联
    /// [Navigate] 仅用于查询，写操作请操作中间表 RoleMenu 或通过领域服务处理
    /// </summary>
    [Navigate(typeof(RoleMenu), nameof(RoleMenu.RoleId), nameof(RoleMenu.MenuId))]
    public List<Menu>? Menus { get; set; }

    /// <summary>
    /// 角色-部门 关联 (用于自定义数据权限)
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(typeof(RoleDepartment), nameof(RoleDepartment.RoleId), nameof(RoleDepartment.DepartmentId))]
    public List<Department>? Depts { get; set; }

    #endregion

    #region 业务方法

    /// <summary>
    /// 更新角色基本信息
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="roleCode">角色编码</param>
    /// <param name="dataScope">数据范围</param>
    /// <param name="remark">备注</param>
    /// <param name="orderNum">排序</param>
    /// <param name="state">状态</param>
    public void Update(string roleName, string roleCode, DataScope dataScope, string? remark, int orderNum, bool state)
    {
        Volo.Abp.Check.NotNullOrWhiteSpace(roleName, nameof(roleName));
        Volo.Abp.Check.NotNullOrWhiteSpace(roleCode, nameof(roleCode));

        RoleName = roleName;
        RoleCode = roleCode;
        DataScope = dataScope;
        Remark = remark;
        OrderNum = orderNum;
        State = state;
    }

    #endregion
}
