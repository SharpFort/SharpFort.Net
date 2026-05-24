using Microsoft.Extensions.DependencyInjection;
using SharpFort.Ai.AgentFramework.Interfaces;

namespace SharpFort.Ai.AgentFramework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIAgentClient(this IServiceCollection services)
    {
        services.AddTransient<IAIAgentService, AIAgentService>();
        return services;
    }
}
