using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
using Yi.Framework.CasbinRbac.Domain.Shared.OperLog;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 操作日志聚合根
    /// 记录系统关键操作行为，用于审计追踪
    /// </summary>
    [SugarTable("sys_operation_log")]
    // 索引1：按时间倒序查询（日志最常用）
    [SugarIndex($"index_{nameof(CreationTime)}", nameof(CreationTime), OrderByType.Desc)]
    // 索引2：按操作人员查询
    [SugarIndex($"index_{nameof(OperUser)}", nameof(OperUser), OrderByType.Asc)]
    // 索引3：按模块标题查询
    [SugarIndex($"index_{nameof(Title)}", nameof(Title), OrderByType.Asc)]
    public class OperationLog : CreationAuditedAggregateRoot<Guid>
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        protected OperationLog() { }

        /// <summary>
        /// 创建操作日志
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="title">操作模块</param>
        /// <param name="operType">操作类型</param>
        /// <param name="method">方法名称(Controller/Action)</param>
        /// <param name="requestMethod">请求方式(GET/POST)</param>
        /// <param name="operUser">操作人员账号</param>
        /// <param name="operIp">操作IP</param>
        /// <param name="operLocation">操作地点</param>
        /// <param name="requestParam">请求参数</param>
        /// <param name="requestResult">返回结果</param>
        public OperationLog(
            Guid id, 
            string title, 
            OperationType operType, 
            string method, 
            string requestMethod, 
            string operUser, 
            string operIp, 
            string? operLocation = null, 
            string? requestParam = null, 
            string? requestResult = null) 
            : base(id)
        {
            Title = title;
            OperType = operType;
            Method = method;
            RequestMethod = requestMethod;
            OperUser = operUser;
            OperIp = operIp;
            OperLocation = operLocation;
            RequestParam = requestParam;
            RequestResult = requestResult;
        }

        #endregion

        #region 核心属性
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 操作模块 / 标题
        /// 如：用户管理、角色管理
        /// </summary>
        [SugarColumn(ColumnName = "Title" ,Length = 64, IsNullable = true)]
        public string? Title { get; protected set; }

        /// <summary>
        /// 操作类型 
        ///</summary>
        [SugarColumn(ColumnName = "OperType")]
        public OperationType OperType { get; protected set; }

        /// <summary>
        /// 请求方式
        /// GET, POST, PUT, DELETE
        /// </summary>
        [SugarColumn(ColumnName = "RequestMethod",Length = 20, IsNullable = true)]
        public string? RequestMethod { get; protected set; }

        /// <summary>
        /// 方法名称
        /// 通常记录 Controller.Action 或 Class.Method
        /// </summary>
        [SugarColumn(ColumnName = "Method",Length = 255, IsNullable = true)]
        public string? Method { get; protected set; }

        /// <summary>
        /// 操作人员账号
        /// (CreatorId 记录的是 Guid，这里记录可读的账号名)
        /// </summary>
        [SugarColumn(ColumnName = "OperUser",Length = 64, IsNullable = true)]
        public string? OperUser { get; protected set; }

        /// <summary>
        /// 操作Ip 
        ///</summary>
        [SugarColumn(ColumnName = "OperIp",Length = 50, IsNullable = true)]
        public string? OperIp { get; protected set; }

        /// <summary>
        /// 操作地点 
        ///</summary>
        [SugarColumn(ColumnName = "OperLocation",Length = 128, IsNullable = true)]
        public string? OperLocation { get; protected set; }

        /// <summary>
        /// 请求参数
        /// 记录 JSON 数据，可能很大
        /// </summary>
        [SugarColumn(ColumnName = "RequestParam",ColumnDataType = "text")]
        public string? RequestParam { get; protected set; }

        /// <summary>
        /// 请求结果
        /// 记录 JSON 数据，可能很大
        /// </summary>
        [SugarColumn(ColumnName = "RequestResult",ColumnDataType = "text")]
        public string? RequestResult { get; protected set; }

        #endregion
    }
}
