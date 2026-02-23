using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using SharpFort.Mapster;
using SharpFort.CasbinRbac.Domain.Shared.Options;

namespace SharpFort.CasbinRbac.Domain.Shared
{
    [DependsOn(typeof(AbpDddDomainSharedModule),
        typeof(SharpFortMapsterModule)
        )]
    public class SharpFortCasbinRbacDomainSharedModule : AbpModule
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