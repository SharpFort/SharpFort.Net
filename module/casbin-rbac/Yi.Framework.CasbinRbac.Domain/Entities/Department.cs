using SqlSugar;
using System.ComponentModel;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.Core.Data;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 部门聚合根
    /// 组织架构核心实体，呈树形结构
    /// </summary>
    [SugarTable("casbin_sys_department")]
    // 部门编码全局唯一
    [SugarIndex($"index_{nameof(DeptCode)}", nameof(DeptCode), OrderByType.Asc, IsUnique = true)]
    // 父级ID索引，加速树形查找（查找子节点时非常频繁）
    [SugarIndex($"index_{nameof(ParentId)}", nameof(ParentId), OrderByType.Asc)]
    // 优化：增加 Ancestors 索引，加速 "本部门及以下" 查询
    [SugarIndex($"index_{nameof(Ancestors)}", nameof(Ancestors), OrderByType.Asc)]
    public class Department : FullAuditedAggregateRoot<Guid>, IOrderNum, IState, IMultiTenant
    {
        #region 构造函数

        public Department() { }

        /// <summary>
        /// 创建新部门
        /// </summary>
        public Department(Guid id, string deptName, string deptCode, Guid parentId, string? ancestors = null, string? leader = null, string? remark = null, int orderNum = 0, bool isDeleted=false)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(deptName, nameof(deptName));
            Volo.Abp.Check.NotNullOrWhiteSpace(deptCode, nameof(deptCode));

            DeptName = deptName;
            DeptCode = deptCode;
            ParentId = parentId;
            Ancestors = ancestors ?? string.Empty; // 默认为空
            Leader = leader;
            Remark = remark;
            OrderNum = orderNum;
            IsDeleted = isDeleted;

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
        /// 部门名称
        /// </summary>
        [SugarColumn(Length = 64)]
        public string DeptName { get; protected set; }

        /// <summary>
        /// 部门编码
        /// 业务唯一标识
        /// </summary>
        [SugarColumn(Length = 64)]
        public string DeptCode { get; protected set; }

        /// <summary>
        /// 父级ID
        /// 根节点通常存储 Guid.Empty
        /// </summary>
        public Guid ParentId { get; protected set; }

        /// <summary>
        /// 祖籍列表
        /// 格式: "0,100,1001" (不包含自身ID)
        /// 用于高效查询所有子节点: Where(d => d.Ancestors.StartsWith("0,100%"))
        /// </summary>
        [SugarColumn(Length = 500)]
        public string Ancestors { get; protected set; }

        /// <summary>
        /// 负责人
        /// 简单场景存姓名，复杂场景建议存 UserId
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 64)]
        public string? Leader { get; protected set; }

        /// <summary>
        /// 备注
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

        #region 树形导航属性 (SqlSugar)

        /// <summary>
        /// 父级部门
        /// [Navigate] 用于向上查找
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(ParentId))]
        public Department? Parent { get; set; }

        /// <summary>
        /// 子部门集合
        /// [Navigate] 用于向下递归查找
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(ParentId))]
        public List<Department> Children { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新部门基本信息
        /// </summary>
        public void Update(string deptName, string deptCode, string? leader, string? remark, int orderNum, bool state)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(deptName, nameof(deptName));
            Volo.Abp.Check.NotNullOrWhiteSpace(deptCode, nameof(deptCode));

            DeptName = deptName;
            DeptCode = deptCode;
            Leader = leader;
            Remark = remark;
            OrderNum = orderNum;
            State = state;
        }

        /// <summary>
        /// 变更父级部门 (移动部门)
        /// 单独的方法，因为这通常涉及复杂的校验（如：不能移动到自己的子节点下）
        /// </summary>
        /// <param name="newParentId">新的父级ID</param>
        /// <param name="newAncestors">新的祖籍路径 (由领域服务计算后传入)</param>
        public void ChangeParent(Guid newParentId, string newAncestors)
        {
            if (newParentId == Id)
            {
                throw new BusinessException("Rbac:Department:CannotBeOwnParent", "部门不能作为自己的父节点");
            }
            ParentId = newParentId;
            Ancestors = newAncestors;
        }

        /// <summary>
        /// 初始化层级路径 (用于创建时)
        /// </summary>
        public void InitPath(Guid parentId, string ancestors)
        {
            ParentId = parentId;
            Ancestors = ancestors;
        }

        #endregion
    }
}

