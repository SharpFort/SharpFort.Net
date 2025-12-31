using SqlSugar;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 第三方授权绑定聚合根
    /// (建议重命名为 OpenAuth 以区分权限逻辑)
    /// </summary>
    [SugarTable("sys_open_auth")]
    // 核心约束：同一个平台下的 OpenId 必须唯一，不能被多个用户绑定
    [SugarIndex($"index_unique_{nameof(AuthType)}_{nameof(OpenId)}", nameof(AuthType),OrderByType.Asc, nameof(OpenId),OrderByType.Asc, IsUnique = true)]
    // 常用查询：查询某个用户绑定了哪些平台
    [SugarIndex($"index_{nameof(UserId)}", nameof(UserId), OrderByType.Asc)]
    public class OpenAuth : FullAuditedAggregateRoot<Guid>
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用
        /// </summary>
        public OpenAuth() { }

        /// <summary>
        /// 创建第三方绑定
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="userId">关联用户ID</param>
        /// <param name="authType">平台类型 (如: Gitee, Github, WeChat)</param>
        /// <param name="openId">第三方唯一标识</param>
        /// <param name="nickName">第三方昵称</param>
        /// <param name="avatar">第三方头像</param>
        /// <param name="token">访问令牌(可选)</param>
        public OpenAuth(Guid id, Guid userId, string authType, string openId, string? nickName = null, string? avatar = null, string? token = null)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(authType, nameof(authType));
            Volo.Abp.Check.NotNullOrWhiteSpace(openId, nameof(openId));

            UserId = userId;
            // 建议：统一转小写存储，避免 "Gitee" 和 "gitee" 造成数据混乱
            AuthType = authType.ToLowerInvariant(); 
            OpenId = openId;
            NickName = nickName;
            Avatar = avatar;
            Token = token;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 关联用户ID
        /// </summary>
        public Guid UserId { get; protected set; }

        /// <summary>
        /// 平台类型
        /// 建议使用常量或字符串 (如: "Gitee", "WeChat")，比枚举更灵活，方便插件化扩展
        /// </summary>
        [SugarColumn(Length = 64)]
        public string AuthType { get; protected set; }

        /// <summary>
        /// 第三方唯一标识 (OpenId)
        /// </summary>
        [SugarColumn(Length = 128)]
        public string OpenId { get; protected set; }

        /// <summary>
        /// 第三方昵称
        /// </summary>
        [SugarColumn(Length = 128, IsNullable = true)]
        public string? NickName { get; set; }

        /// <summary>
        /// 第三方头像
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Avatar { get; set; }

        /// <summary>
        /// 访问令牌 (Access Token)
        /// 用于后续调用第三方接口，可选存储
        /// </summary>
        [SugarColumn(Length = 2000, IsNullable = true)]
        public string? Token { get; set; }

        #endregion

        #region 导航属性

        /// <summary>
        /// 关联的用户
        /// [Navigate] 仅用于查询
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        public User? User { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新令牌信息
        /// (当用户重新登录时，更新Token)
        /// </summary>
        public void UpdateToken(string token, string? nickName, string? avatar)
        {
            if (!string.IsNullOrWhiteSpace(token)) Token = token;
            if (!string.IsNullOrWhiteSpace(nickName)) NickName = nickName;
            if (!string.IsNullOrWhiteSpace(avatar)) Avatar = avatar;
        }

        #endregion
    }
}