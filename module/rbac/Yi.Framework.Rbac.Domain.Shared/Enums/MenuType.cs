using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.Rbac.Domain.Shared.Enums
{
    public enum MenuType
    {
        /// <summary>
        /// 目录
        /// </summary>
        [Description("目录")]
        Catalogue = 0,

        /// <summary>
        /// 菜单
        /// </summary>
        [Description("菜单")]
        Menu = 1,

        /// <summary>
        /// 组件 (特殊)
        /// </summary>
        [Description("组件")]
        Component = 2,

        /// <summary>
        /// 按钮/权限点
        /// </summary>
        [Description("按钮")]
        Button = 3
    }
}
