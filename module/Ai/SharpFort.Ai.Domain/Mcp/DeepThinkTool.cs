using System.ComponentModel;
using ModelContextProtocol.Server;
using Volo.Abp.DependencyInjection;
using SharpFort.Ai.Domain.Shared.Attributes;

namespace SharpFort.Ai.Domain.Mcp;

[SfAgentTool]
public class DeepThinkTool:ISingletonDependency
{
    [SfAgentTool("深度思考"),DisplayName("DeepThink"),Description("进行深度思考")]
    public void DeepThink()
    {
        
    }
}
