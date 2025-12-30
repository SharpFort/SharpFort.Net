using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Yi.Framework.Bbs.Domain.Shared.Enums;

namespace Yi.Framework.Bbs.Domain.Entities
{
    [SugarTable("bbs_notice")]
    public class BbsNotice : AggregateRoot<Guid>, IHasCreationTime
    {
        public BbsNotice()
        { 
        
        }
        public BbsNotice(NoticeType noticeType, string message, Guid? acceptUserId = null)
        {
            this.NoticeType = noticeType;
            this.Message = message;
            this.AcceptUserId = acceptUserId;

        }
        /// <summary>
        /// 设置已读
        /// </summary>
        public void SetRead()
        {
            IsRead = true;
            this.ReadTime = DateTime.Now;
        }


        public Guid? AcceptUserId { get; }

        /// <summary>
        /// 消息,支持html
        /// </summary>
        [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string Message { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public NoticeType NoticeType { get; }

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; private set; }

        /// <summary>
        /// 已读时间
        /// </summary>
        public DateTime? ReadTime { get; private set; }

        public DateTime CreationTime { get; set; }
    }


}
