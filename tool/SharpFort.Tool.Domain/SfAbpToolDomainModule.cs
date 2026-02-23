using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpFort.Tool.Domain.Shared;
using SharpFort.Tool.Domain.Shared.Options;

namespace SharpFort.Tool.Domain
{
    [DependsOn(typeof(SfAbpToolDomainSharedModule))]
    public class SfAbpToolDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ToolOptions>(configuration.GetSection("ToolOptions"));
            var toolOptions = new ToolOptions();
            configuration.GetSection("ToolOptions").Bind(toolOptions);
            if (!Directory.Exists(toolOptions.TempDirPath))
            {
                Directory.CreateDirectory(toolOptions.TempDirPath);
            }
            
        }
    }
}
