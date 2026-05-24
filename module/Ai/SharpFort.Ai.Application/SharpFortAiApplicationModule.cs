using Microsoft.Extensions.DependencyInjection;
using SharpFort.Ai.Application.Contracts;
using SharpFort.Ai.Domain;
using SharpFort.Ddd.Application;
using SharpFort.Ai.AgentFramework.Interfaces;
using SharpFort.Ai.AgentFramework.Interfaces.Tasks;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Application.Services;
using SharpFort.Ai.AgentFramework;

namespace SharpFort.Ai.Application
{
    [DependsOn(
        typeof(SharpFortAiApplicationContractsModule),
        typeof(SharpFortAiDomainModule),

        typeof(SharpFortDddApplicationModule)

    )]
    public class SharpFortAiApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // Agent Framework
            context.Services.AddAIAgentClient();

            // Application Services
            context.Services.AddTransient<IAiAppService, AiAppService>();
            context.Services.AddTransient<IAiSkillToolService, AiSkillToolService>();
            context.Services.AddTransient<IAiSkillToolBindService, AiSkillToolBindService>();
            context.Services.AddTransient<IAiKmsService, AiKmsService>();
            context.Services.AddTransient<IAIAgentToolSkillService, AiAgentToolSkillService>();
            context.Services.AddTransient<IKevinAIChatMessageStore, AiChatMessageStoreService>();
            context.Services.AddTransient<IKevinAITaskService, KevinAITasksService>();
        }
    }
}
