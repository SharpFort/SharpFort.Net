using Lazy.Captcha.Core.Generator;
using Microsoft.Extensions.DependencyInjection;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts;
using Yi.Framework.CasbinRbac.Domain;

namespace Yi.Framework.CasbinRbac.Application
{
    [DependsOn(
        typeof(YiFrameworkCasbinRbacApplicationContractsModule),
        typeof(YiFrameworkCasbinRbacDomainModule),


        typeof(YiFrameworkDddApplicationModule)
        )]
    public class YiFrameworkCasbinRbacApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var service = context.Services;

            service.AddCaptcha(options =>
            {
                options.CaptchaType = CaptchaType.ARITHMETIC;
            });
        }

        public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
        }
    }
}
