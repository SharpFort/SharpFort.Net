using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Yi.Framework.Mapster;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;

namespace Yi.Framework.CasbinRbac.Domain.Shared
{
    [DependsOn(typeof(AbpDddDomainSharedModule),
        typeof(YiFrameworkMapsterModule)
        )]
    public class YiFrameworkCasbinRbacDomainSharedModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
            Configure<RefreshJwtOptions>(configuration.GetSection(nameof(RefreshJwtOptions)));
            Configure<RbacOptions>(configuration.GetSection(nameof(RbacOptions)));
        }
    }
}