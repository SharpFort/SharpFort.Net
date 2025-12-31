using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.Core.Data;

namespace Yi.Framework.CasbinRbac.Domain.Entities;

/// <summary>
/// 岗位聚合根
/// </summary>
[SugarTable("sys_position")]
// 岗位编码必须唯一
[SugarIndex($"index_{nameof(PostCode)}", nameof(PostCode), OrderByType.Asc, IsUnique = true)]
public class Position : FullAuditedAggregateRoot<Guid>, IOrderNum, IState, IMultiTenant
{
    #region 构造函数

    /// <summary>
    /// ORM 专用无参构造函数
    /// 必须为 protected，防止业务代码错误创建空对象
    /// </summary>
    public Position() { }

    /// <summary>
    /// 创建新岗位
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="postCode">岗位编码</param>
    /// <param name="postName">岗位名称</param>
    /// <param name="remark">备注</param>
    /// <param name="orderNum">排序</param>
    public Position(Guid id, string postCode, string postName, string? remark = null, int orderNum = 0)
        : base(id)
    {
        Volo.Abp.Check.NotNullOrWhiteSpace(postCode, nameof(postCode));
        Volo.Abp.Check.NotNullOrWhiteSpace(postName, nameof(postName));

        PostCode = postCode;
        PostName = postName;
        Remark = remark;
        OrderNum = orderNum;

        // 默认状态
        State = true;
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
    /// 岗位编码
    /// 核心业务字段，创建后一般不允许随意变更，修改需通过特定业务流程
    /// </summary>
    [SugarColumn(Length = 64)]
    public string PostCode { get; protected set; }

    /// <summary>
    /// 岗位名称
    /// </summary>
    [SugarColumn(Length = 64)]
    public string PostName { get; protected set; }

    /// <summary>
    /// 备注
    /// 非核心校验字段，但为了统一修改入口，建议也保持 protected set
    /// </summary>
    [SugarColumn(IsNullable = true, Length = 500)]
    public string? Remark { get; protected set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public bool State { get; set; }

    #endregion

    #region 业务方法

    /// <summary>
    /// 更新岗位信息
    /// 提供统一的修改入口，方便将来添加校验逻辑（如：名称不能包含敏感词）
    /// </summary>
    /// <param name="postName">岗位名称</param>
    /// <param name="postCode">岗位编码</param>
    /// <param name="remark">备注</param>
    /// <param name="orderNum">排序</param>
    /// <param name="state">状态</param>
    public void Update(string postName, string postCode, string? remark, int orderNum, bool state)
    {
        Volo.Abp.Check.NotNullOrWhiteSpace(postName, nameof(postName));
        Volo.Abp.Check.NotNullOrWhiteSpace(postCode, nameof(postCode));

        PostName = postName;
        PostCode = postCode;
        Remark = remark;
        OrderNum = orderNum;
        State = state;
    }

    /// <summary>
    /// 启用/禁用岗位
    /// 比直接 set State = true 更有语义
    /// </summary>
    /// <param name="state">状态</param>
    public void SetState(bool state)
    {
        State = state;
    }

    #endregion
}
