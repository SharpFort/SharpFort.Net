using System.ComponentModel;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Attributes;

namespace SharpFort.Ai.Domain.Mcp;

[SfAgentTool]
[Obsolete("Planned for removal. Assess if Agent infrastructure is needed.")]
public class DateTimeTool : ISingletonDependency
{
    [SfAgentTool("时间"), DisplayName("DateTime"), Description("获取当前日期与时间")]
    public static DateTime DateTime()
    {
        return System.DateTime.Now;
    }
}
