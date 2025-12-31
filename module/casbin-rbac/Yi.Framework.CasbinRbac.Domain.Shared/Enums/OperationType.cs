using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yi.Framework.CasbinRbac.Domain.Shared.Enums
{
    public enum OperationType 
    {
        [Description("其他")]
        Other = 0,

        [Description("新增")]
        Insert = 1,

        [Description("修改")]
        Update = 2,

        [Description("删除")]
        Delete = 3,

        [Description("授权")]
        Auth = 4,

        [Description("导出")]
        Export = 5,

        [Description("导入")]
        Import = 6,

        [Description("强退")]
        Force = 7,

        [Description("生成代码")]
        GenerateCode = 8,

        [Description("清空数据")]
        Clean = 9
    }
}
