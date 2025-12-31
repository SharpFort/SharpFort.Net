using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.CasbinRbac.Domain.Shared.Enums
{
    /// <summary>
    /// 数据权限范围
    /// </summary>
    public enum DataScope
    {
        /// <summary>
        /// 全部数据权限
        /// </summary>
        ALL = 0,

        /// <summary>
        /// 自定义数据权限
        /// </summary>
        CUSTOM = 1,

        /// <summary>
        /// 部门数据权限
        /// </summary>
        DEPT = 2,

        /// <summary>
        /// 部门及以下数据权限
        /// </summary>
        DEPT_FOLLOW = 3,

        /// <summary>
        /// 仅本人数据权限
        /// </summary>
        SELF = 4
    }
}
