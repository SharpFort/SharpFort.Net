using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.Rbac.Domain.Entities;
/// <summary>
/// 用户-岗位关联实体
/// 用于存储用户担任的岗位信息
/// </summary>
[SugarTable("sys_user_position")]
// 核心约束：防止重复分配同一岗位，建立联合唯一索引
[SugarIndex("index_user_position_unique", nameof(UserId),OrderByType.Asc, nameof(PostId), OrderByType.Asc, IsUnique = true)]
public class UserPosition : CreationAuditedEntity<Guid>
{
    #region 构造函数

    /// <summary>
    /// ORM 专用无参构造函数
    /// </summary>
    public UserPosition() { }

    /// <summary>
    /// 创建关联
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">岗位ID</param>
    public UserPosition(Guid userId, Guid postId)
    {
        UserId = userId;
        PostId = postId;
    }

    /// <summary>
    /// 构造函数重载 (带ID)
    /// </summary>
    public UserPosition(Guid id, Guid userId, Guid postId) : base(id)
    {
        UserId = userId;
        PostId = postId;
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
    /// 岗位ID
    /// </summary>
    public Guid PostId { get;  set; }

    #endregion

    #region 导航属性 (仅供查询)

    /// <summary>
    /// 关联的用户
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 关联的岗位
    /// [Navigate] 仅用于查询
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(PostId))]
    public Position? Position { get; set; }

    #endregion
}