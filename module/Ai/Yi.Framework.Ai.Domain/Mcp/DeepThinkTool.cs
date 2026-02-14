using System.ComponentModel;
using ModelContextProtocol.Server;
using Volo.Abp.DependencyInjection;
using Yi.Framework.Ai.Domain.Shared.Attributes;

namespace Yi.Framework.Ai.Domain.Mcp;

[YiAgentTool]
public class DeepThinkTool:ISingletonDependency
{
    [YiAgentTool("深度思考"),DisplayName("DeepThink"),Description("进行深度思考")]
    public void DeepThink()
    {
        
    }
}
