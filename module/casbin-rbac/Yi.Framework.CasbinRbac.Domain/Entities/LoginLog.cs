using Microsoft.AspNetCore.Http;
using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Yi.Framework.Core.Extensions;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    /// <summary>
    /// 登录日志聚合根
    /// 记录用户登录行为，数据不可变（仅记录创建信息）
    /// </summary>
    [SugarTable("sys_login_log")]
    // 索引1：按用户查询
    [SugarIndex($"index_{nameof(LoginUser)}", nameof(LoginUser), OrderByType.Asc)]
    // 索引2：按时间范围查询（日志查询最常用）
    [SugarIndex($"index_{nameof(CreationTime)}", nameof(CreationTime), OrderByType.Desc)]
    public class LoginLog : CreationAuditedAggregateRoot<Guid>
    {
        #region 构造函数

        /// <summary>
        /// ORM 专用无参构造函数
        /// </summary>
        public LoginLog() { }

        /// <summary>
        /// 创建登录日志
        /// </summary>
        /// <param name="id">主键</param>
        /// <param name="loginUser">尝试登录的用户名/账号</param>
        /// <param name="loginIp">IP地址</param>
        /// <param name="loginLocation">地理位置</param>
        /// <param name="os">操作系统</param>
        /// <param name="browser">浏览器</param>
        /// <param name="logMsg">日志消息/结果</param>
        /// <param name="isSuccess">是否登录成功(可选扩展)</param>
        public LoginLog(
            Guid id,
            string loginUser,
            string loginIp,
            string? loginLocation,
            string? os,
            string? browser,
            Guid? creatorId,
            string? logMsg)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(loginUser, nameof(loginUser));

            LoginUser = loginUser;
            LoginIp = loginIp;
            LoginLocation = loginLocation;
            Os = os;
            Browser = browser;
            LogMsg = logMsg;

            // CreationTime 由基类自动设置，但如果是通过 Log 组件异步批量插入，
            // 有时可能需要手动指定时间，这里保持框架默认行为即可。
            CreatorId = creatorId;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 登录账号/用户名
        /// (注意：CreatorId 记录的是登录成功的 UserId，而这里记录的是尝试登录的输入串)
        /// </summary>
        [SugarColumn(Length = 64)]
        public string LoginUser { get; protected set; }

        /// <summary>
        /// 登录IP
        /// </summary>
        [SugarColumn(Length = 50)]
        public string? LoginIp { get; protected set; }

        /// <summary>
        /// 登录地点
        /// </summary>
        [SugarColumn(Length = 128)]
        public string? LoginLocation { get; protected set; }

        /// <summary>
        /// 浏览器
        /// </summary>
        [SugarColumn(Length = 128)]
        public string? Browser { get; protected set; }

        /// <summary>
        /// 操作系统
        /// </summary>
        [SugarColumn(Length = 128)]
        public string? Os { get; protected set; }

        /// <summary>
        /// 日志消息/状态描述
        /// 如："登录成功"、"密码错误"
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? LogMsg { get; protected set; }

        // 建议：后续可考虑添加 Status 字段 (bool IsSuccess) 以便统计成功率

        #endregion

        // 注意：原有的 GetInfoByHttpContext 方法已移除。
        // 原因：实体层不应依赖 HttpContext。请在 Application Service 中解析完 UserAgent 后，
        // 直接通过构造函数传入 clean data。
    }
}


//public LoginLog GetInfoByHttpContext(HttpContext context)
//        {
//            ClientInfo GetClientInfo(HttpContext context)
//            {
//                var str = context.GetUserAgent();
//                var uaParser = Parser.GetDefault();
//                ClientInfo c;
//                try
//                {
//                     c = uaParser.Parse(str);
//                }
//                catch
//                {
//                    c = new ClientInfo("null",new OS("null", "null", "null", "null", "null"),new Device("null","null","null"), new UserAgent("null", "null", "null", "null"));
//                }
//                return c;
//            }
//            var ipAddr = context.GetClientIp();
//            IpInfo location;
//            if (ipAddr == "127.0.0.1")
//            {
//                location = new IpInfo() { Province = "本地", City = "本机" };
//            }
//            else
//            {
//                try
//                {
//                    location = IpTool.Search(ipAddr);
//                }
//                catch
//                {
//                    location = new IpInfo() { Province = ipAddr, City = "未知地区" };
//                }
//            }
//            ClientInfo clientInfo = GetClientInfo(context);
//            LoginLog entity = new()
//            {
//                Browser = clientInfo.Device.Family,
//                Os = clientInfo.OS.ToString(),
//                LoginIp = ipAddr,
//                LoginLocation = location.Province + "-" + location.City
//            };
//            return entity;
//        }
//    }

//}
