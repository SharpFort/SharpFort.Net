using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using SharpFort.Tool.HttpApi.Client;

namespace SharpFort.Tool
{
    [DependsOn(typeof(SfAbpToolHttpApiClientModule)
        )]
    public class SfAbpToolModule : AbpModule
    {

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
           // var configuration = context.Services.GetConfiguration();
            Configure<AbpRemoteServiceOptions>(options =>
            {
                options.RemoteServices.Default =
                     new RemoteServiceConfiguration("https://ccnetcore.com:19009");
                   // new RemoteServiceConfiguration("http://localhost:19002");
            });
        }
    }
}
