using System.Collections.Generic;

namespace FluidSequence.Domain.Services
{
    public class PlaceholderMeta
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
    }

    public static class PlaceholderRegistry
    {
        public static readonly List<PlaceholderMeta> Definitions = new List<PlaceholderMeta>
        {
            new PlaceholderMeta { Key = "{yyyy}", Label = "年份(4位)", Group = "时间" },
            new PlaceholderMeta { Key = "{yy}", Label = "年份(2位)", Group = "时间" },
            new PlaceholderMeta { Key = "{MM}", Label = "月份", Group = "时间" },
            new PlaceholderMeta { Key = "{dd}", Label = "日期", Group = "时间" },
            new PlaceholderMeta { Key = "{HH}", Label = "小时", Group = "时间" },
            new PlaceholderMeta { Key = "{mm}", Label = "分钟", Group = "时间" },
            new PlaceholderMeta { Key = "{ss}", Label = "秒", Group = "时间" },
            new PlaceholderMeta { Key = "{ww}", Label = "周数", Group = "时间" },
            new PlaceholderMeta { Key = "{QQ}", Label = "季度", Group = "时间" },
            new PlaceholderMeta { Key = "{FY}", Label = "财年", Group = "时间" },
            
            new PlaceholderMeta { Key = "{SEQ}", Label = "序列号", Group = "核心" },
            new PlaceholderMeta { Key = "{SEQ36}", Label = "36进制序列号", Group = "核心" },
            
            new PlaceholderMeta { Key = "{RAND:NUM:4}", Label = "4位随机数字", Group = "随机" },
            new PlaceholderMeta { Key = "{RAND:CHAR:4}", Label = "4位随机字母", Group = "随机" },
            new PlaceholderMeta { Key = "{RAND:SAFE:4}", Label = "4位安全随机码", Group = "随机" },
            new PlaceholderMeta { Key = "{RAND:MIX:4}", Label = "4位混合随机码", Group = "随机" },

            new PlaceholderMeta { Key = "{UserCode}", Label = "用户编码", Group = "上下文" },
            new PlaceholderMeta { Key = "{DeptCode}", Label = "部门编码", Group = "上下文" },
            new PlaceholderMeta { Key = "{TenantCode}", Label = "租户编码", Group = "上下文" },
        };
    }
}
