using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Rbac.Domain.Shared.Enums
{
    public enum JobType
    {
        /// <summary>
        /// 未知/默认
        /// </summary>
        [Description("未知")]
        None = 0,

        /// <summary>
        /// Cron表达式
        /// </summary>
        [Description("Cron表达式")]
        Cron = 1,

        /// <summary>
        /// 毫秒延时
        /// </summary>
        [Description("毫秒延时")]
        Millisecond = 2
    }
}
