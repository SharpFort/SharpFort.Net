using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using SharpFort.Core;
using SharpFort.WeChat.MiniProgram.Token;
using Microsoft.Extensions.Configuration;

namespace SharpFort.WeChat.MiniProgram;

[DependsOn(typeof(SharpFortCoreModule),
    typeof(AbpCachingModule))]
public class SharpFortWeChatMiniProgramModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;
        IConfiguration configuration = context.Services.GetConfiguration();
        Configure<WeChatMiniProgramOptions>(configuration.GetSection("WeChatMiniProgram"));
        services.AddSingleton<IMiniProgramToken, CacheMiniProgramToken>();
    }
}