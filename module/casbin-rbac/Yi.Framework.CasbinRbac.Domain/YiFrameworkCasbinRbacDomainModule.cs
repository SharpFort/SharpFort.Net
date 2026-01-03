using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain;
using Volo.Abp.Imaging;
using Volo.Abp.Modularity;
using Yi.Framework.Caching.FreeRedis;
using Yi.Framework.Mapster;
using Yi.Framework.CasbinRbac.Domain.Operlog;
using Yi.Framework.CasbinRbac.Domain.Shared;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;

namespace Yi.Framework.CasbinRbac.Domain
{
    [DependsOn(
        typeof(YiFrameworkCasbinRbacDomainSharedModule),
        typeof(YiFrameworkCachingFreeRedisModule),

        typeof(AbpAspNetCoreSignalRModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(AbpImagingImageSharpModule),
        typeof(AbpDistributedLockingModule)
        )]
    public class YiFrameworkCasbinRbacDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var service = context.Services;
            var configuration = context.Services.GetConfiguration();
            service.AddControllers(options =>
            {
                options.Filters.Add<OperLogGlobalAttribute>();
            });

            //配置阿里云短信
            Configure<AliyunOptions>(configuration.GetSection(nameof(AliyunOptions)));
            
            // 配置 Casbin 选项 (SuperAdminRoleCode, DebugMode)
            Configure<CasbinOptions>(configuration.GetSection("Casbin"));
            
            //分布式锁,需要redis
            if (configuration.GetSection("Redis").GetValue<bool>("IsEnabled"))
            {
                context.Services.AddSingleton<IDistributedLockProvider>(sp =>
                {
                    var connection = ConnectionMultiplexer
                        .Connect(configuration["Redis:Configuration"]);
                    return new 
                        RedisDistributedSynchronizationProvider(connection.GetDatabase());
                });
            }

        }
    }
}
