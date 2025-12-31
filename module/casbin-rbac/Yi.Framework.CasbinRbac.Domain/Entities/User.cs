using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.Core.Data;
using Yi.Framework.Core.Helper;
using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 用户聚合根
    /// 核心业务实体，继承 FullAuditedAggregateRoot 以支持完整的审计（创建/修改/软删除）和并发控制。
    /// </summary>
    [SugarTable("sys_user")]
    // 用户名必须唯一，使用唯一索引
    [SugarIndex($"index_{nameof(UserName)}", nameof(UserName), OrderByType.Asc, IsUnique = true)]
    // 经常通过手机号查询，建议加索引
    [SugarIndex($"index_{nameof(Phone)}", nameof(Phone), OrderByType.Asc)]
    public class User : FullAuditedAggregateRoot<Guid>, IOrderNum, IState, IMultiTenant
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public User() { }

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">明文密码</param>
        /// <param name="phone">电话</param>
        /// <param name="nick">昵称</param>
        public User(string userName, string password, long? phone = null, string? nick = null)
           
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(userName, nameof(userName));
            Volo.Abp.Check.NotNullOrWhiteSpace(password, nameof(password));

            UserName = userName;
            Phone = phone;
            // 默认昵称逻辑：若未提供，则为 "萌新-用户名"
            Nick = string.IsNullOrWhiteSpace(nick) ? $"萌新-{userName}" : nick.Trim();

            // 设置默认值
            State = true;
            OrderNum = 0;
            Gender = Gender.Unknown;

            // 设置初始密码
            SetPassword(password);
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键 (重写以适配 SqlSugar 主键特性)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }
        
        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid? TenantId { get; protected set; }

        /// <summary>
        /// 用户名
        /// 业务主键，不可变
        /// </summary>
        public string UserName { get; protected set; }

        /// <summary>
        /// 密码哈希值
        /// 存储 BCrypt 加密后的字符串
        /// </summary>
        [SugarColumn(Length = 128)]
        public string Password { get; protected set; }

        /// <summary>
        /// 姓名 (实名)
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Name { get; protected set; } = string.Empty;

        /// <summary>
        /// 昵称
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Nick { get; protected set; }

        /// <summary>
        /// 电话
        /// 建议统一使用 String 存储电话以保留前导零，此处保留 long 兼容旧设计
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public long? Phone { get; protected set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Email { get; protected set; }

        /// <summary>
        /// 头像地址
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Icon { get;  set; }

        /// <summary>
        /// 性别
        /// </summary>
        [SugarColumn(IsNullable = true,ColumnDataType = "string")]
        public Gender Gender { get; protected set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; protected set; }

        #endregion

        #region 扩展信息

        /// <summary>
        /// 登录IP
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Ip { get; protected set; }

        /// <summary>
        /// 地址
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Address { get; protected set; }

        /// <summary>
        /// 个人简介
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 500)]
        public string? Introduction { get; protected set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 500)]
        public string? Remark { get; protected set; }

        /// <summary>
        /// 排序号 (IOrderNum 实现)
        /// </summary>
        public int OrderNum { get;  set; }

        /// <summary>
        /// 启用状态 (IState 实现)
        /// </summary>
        public bool State { get;  set; }

        #endregion

        #region 聚合关系与导航 (SqlSugar)

        /// <summary>
        /// 部门ID
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public Guid? DepartmentId { get; protected set; }

        /// <summary>
        /// 部门导航属性
        /// [Navigate] 仅用于查询（Read Model），写操作请通过 DepartmentId 处理
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(DepartmentId))]
        public Department? Dept { get;  set; }

        /// <summary>
        /// 角色集合
        /// 跨聚合关系，通过中间表 UserRole 连接
        /// </summary>
        [Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId))]
        public List<Role> Roles { get;  set; }

        /// <summary>
        /// 岗位集合
        /// 跨聚合关系，通过中间表 UserPosition 连接
        /// </summary>
        [Navigate(typeof(UserPosition), nameof(UserPosition.UserId), nameof(UserPosition.PostId))]
        public List<Position> Posts { get;  set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置/重置密码
        /// 使用 BCrypt 算法加密，内置盐值
        /// </summary>
        /// <param name="rawPassword">明文密码</param>
        public void SetPassword(string rawPassword)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(rawPassword, nameof(rawPassword));
            // BCrypt 自动处理盐值，workFactor 默认为 11-12 之间较为安全且性能适中
            Password = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);
        }

        /// <summary>
        /// 验证密码
        /// 支持 BCrypt 新格式验证，同时兼容旧版 Salt+MD5 验证
        /// </summary>
        /// <param name="rawPassword">明文密码</param>
        /// <returns>是否验证通过</returns>
        public bool VerifyPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(rawPassword))
            {
                return false;
            }

            // 1. 标准 BCrypt 验证 (以 $2a$, $2b$, $2y$ 开头)
            if (Password.StartsWith("$2"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(rawPassword, Password);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 验证并自动升级密码加密方式
        /// 用户登录成功后调用此方法，平滑迁移旧哈希到 BCrypt
        /// </summary>
        /// <param name="rawPassword">明文密码</param>
        /// <returns>密码是否匹配</returns>
        public bool VerifyAndUpgradePassword(string rawPassword)
        {
            // 1. 验证密码
            if (!VerifyPassword(rawPassword))
            {
                return false;
            }

            // 2. 检查是否需要升级 (如果不是 BCrypt 格式)
            if (!Password.StartsWith("$2"))
            {
                SetPassword(rawPassword);
                // 注意：调用方（应用服务）需要执行 Repository.UpdateAsync(user) 以持久化新密码
            }

            return true;
        }

        #endregion

    }
}
