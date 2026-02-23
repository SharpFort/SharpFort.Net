using System.ComponentModel;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Attributes;

namespace SharpFort.Ai.Domain.Mcp;

[SfAgentTool]
public class DateTimeTool:ISingletonDependency
{
    [SfAgentTool("时间"), DisplayName("DateTime"), Description("获取当前日期与时间")]
    public DateTime DateTime()
    {
        return System.DateTime.Now;
    }
}
