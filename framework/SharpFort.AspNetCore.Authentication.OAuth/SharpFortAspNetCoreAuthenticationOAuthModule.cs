using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using SharpFort.Core;


namespace SharpFort.AspNetCore.Authentication.OAuth
{
    /// <summary>
    /// 本模块轮子来自 AspNet.Security.OAuth.QQ;
    /// </summary>
    [DependsOn(typeof(SharpFortAspNetCoreModule))]
    public class SharpFortAspNetCoreAuthenticationOAuthModule:AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var service = context.Services;
            service.AddHttpClient();
        }
    }
}
