using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using SharpFort.Core;
using SharpFort.WeChat.MiniProgram.Token;

namespace SharpFort.WeChat.MiniProgram;

[DependsOn(typeof(SharpFortCoreModule),
    typeof(AbpCachingModule))]
public class SharpFortWeChatMiniProgramModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Services.GetConfiguration();
        Configure<WeChatMiniProgramOptions>(configuration.GetSection("WeChatMiniProgram"));
        services.AddSingleton<IMiniProgramToken, CacheMiniProgramToken>();
    }
}