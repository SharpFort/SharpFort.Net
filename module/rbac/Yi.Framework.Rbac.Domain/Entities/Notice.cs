using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Data;
using Yi.Framework.Rbac.Domain.Shared.Enums;

namespace Yi.Framework.Rbac.Domain.Entities
{
    /// <summary>
    /// 通知公告聚合根
    /// 用于管理系统通知、公告内容的发布
    /// </summary>
    [SugarTable("sys_notice")]
    // 索引1：查询启用状态的公告 (高频：查询首页显示的公告)
    [SugarIndex($"index_{nameof(State)}", nameof(State), OrderByType.Asc)]
    // 索引2：按类型筛选
    [SugarIndex($"index_{nameof(Type)}", nameof(Type), OrderByType.Asc)]
    public class Notice : FullAuditedAggregateRoot<Guid>, IOrderNum, IState
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public Notice() { }

        /// <summary>
        /// 创建通知公告
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="title">标题</param>
        /// <param name="type">类型</param>
        /// <param name="content">内容(富文本)</param>
        /// <param name="orderNum">排序</param>
        /// <param name="state">状态(默认发布或草稿)</param>
        public Notice(Guid id, string title, NoticeType type, string content, int orderNum = 0, bool state = true)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(title, nameof(title));
            // Content 允许为空吗？通常不允许，但也取决于业务
            Volo.Abp.Check.NotNullOrWhiteSpace(content, nameof(content));

            Title = title;
            Type = type;
            Content = content;
            OrderNum = orderNum;
            State = state;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 公告标题
        /// </summary>
        [SugarColumn(Length = 128)]
        public string Title { get; protected set; }

        /// <summary>
        /// 公告类型
        /// </summary>
        public NoticeType Type { get; protected set; }

        /// <summary>
        /// 公告内容
        /// 存储 HTML 富文本，使用 text 类型以支持大数据量
        /// </summary>
        [SugarColumn(ColumnDataType = "text", IsNullable = false)]
        // 如果您的框架有 StaticConfig.CodeFirst_BigString，也可以保持原样，但 "text" 对 PGSQL 更通用
        public string Content { get; protected set; }

        /// <summary>
        /// 排序 (IOrderNum 实现)
        /// </summary>
        public int OrderNum { get; set; }

        /// <summary>
        /// 状态 (IState 实现)
        /// True: 发布, False: 关闭/草稿
        /// </summary>
        public bool State { get; set; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新公告内容
        /// </summary>
        public void Update(string title, NoticeType type, string content, int orderNum, bool state)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(title, nameof(title));
            Volo.Abp.Check.NotNullOrWhiteSpace(content, nameof(content));

            Title = title;
            Type = type;
            Content = content;
            OrderNum = orderNum;
            State = state;
        }

        /// <summary>
        /// 发布公告
        /// </summary>
        public void Publish()
        {
            State = true;
        }

        /// <summary>
        /// 撤回/关闭公告
        /// </summary>
        public void Withdraw()
        {
            State = false;
        }

        #endregion
    }
}