using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Yi.Framework.Bbs.Domain.Shared.Enums;

namespace Yi.Framework.Bbs.Domain.Entities
{
    /// <summary>
    /// bbs用户表
    /// </summary>
    [SugarTable("bbs_user_extrainfo")]
    [SugarIndex($"index_{nameof(UserId)}", nameof(UserId), OrderByType.Asc)]
    public class BbsUserExtraInfo : Entity<Guid>
    {
        public BbsUserExtraInfo() { }

        public BbsUserExtraInfo(Guid userId) { this.UserId = userId; }

        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 用户id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户限制
        /// </summary>
        public UserSafetyStatus UserLimit { get; set; } = UserSafetyStatus.Normal;


        /// <summary>
        /// 用户等级
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 钱钱
        /// </summary>
        public decimal Money { get; set; } = 0m;


        /// <summary>
        /// 经验
        /// </summary>
        public long Experience { get; set; } = 0;

        /// <summary>
        /// 发表主题数
        /// </summary>
        public int DiscussNumber { get; set; }

        /// <summary>
        /// 发表主题数
        /// </summary>
        public int CommentNumber { get; set; }


        /// <summary>
        /// 被点赞数
        /// </summary>
        public int AgreeNumber { get; set; }
    }

}
