using System.ComponentModel;
using Volo.Abp.DependencyInjection;
using Yi.Framework.Ai.Domain.Shared.Attributes;

namespace Yi.Framework.Ai.Domain.Mcp;

[YiAgentTool]
public class DateTimeTool:ISingletonDependency
{
    [YiAgentTool("时间"), DisplayName("DateTime"), Description("获取当前日期与时间")]
    public DateTime DateTime()
    {
        return System.DateTime.Now;
    }
}
