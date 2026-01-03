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
            
            // 注册字段级权限控制器
            context.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            {
                // 注意：需要确保 IHttpContextAccessor 已注册 (ABP 默认已注册)
                // 这里我们不能直接 new Factory(accessor)，因为 Configure 时还没有 provider。
                // 只能添加 Type 或者 Instance。System.Text.Json 不支持通过 DI 解析 Converter，除非我们用 MvcNewtonsoftJson 的那套 logic。
                // 但 System.Text.Json 的 Converters 是 List<JsonConverter>。
                // 既然 Converter 依赖 Service，我们不能直接在这里 Add(new factory)。
                // 这是一个常见痛点。
                
                // 解决方案：不要在这里注册。
                // 而是实现 IConfigureOptions<JsonOptions> 并在那里注入 IHttpContextAccessor。
                // 或者，FieldSecurityJsonConverterFactory 不依赖 Constructor Argument，而是依赖 ServiceProvider (在 CreateConverter 传入 options? 不行)
                // 我们的 Factory 依赖 IHttpContextAccessor。
                
                // 回退一步：Factory 不应该依赖 Accessor。Factory 取 Accessor 应该在 CreateConverter 里面吗？
                // CreateConverter 只有 Type 和 Options。
                // 实际上，Converter 实例是新建的。Converter 自身可以持有 Accessor。
                // 只要 Factory 能拿到 Accessor。
                
                // 最佳实践：在 ApplicationInitialization 或 Web 层注册？
                // 或者，让 Factory 只有无参构造函数，然后使用 `new HttpContextAccessor()`? 不行。
                
                // 既然我们在 Abp 框架下，我们可以使用 `IConfigureOptions`.
            });

            // 注册配置服务，通过 DI 注入依赖
            context.Services.AddTransient<Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.JsonOptions>, JsonOptionsSetup>();
        }

        public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
        }
    }
}
