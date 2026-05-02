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
using SharpFort.Caching.FreeRedis;
using SharpFort.Mapster;
using SharpFort.CasbinRbac.Domain.Operlog;
using SharpFort.CasbinRbac.Domain.Shared;
using SharpFort.CasbinRbac.Domain.Shared.Options;

namespace SharpFort.CasbinRbac.Domain
{
    [DependsOn(
        typeof(SharpFortCasbinRbacDomainSharedModule),
        typeof(SharpFortCachingFreeRedisModule),

        typeof(AbpAspNetCoreSignalRModule),
        typeof(AbpDddDomainModule),
        typeof(AbpCachingModule),
        typeof(AbpImagingImageSharpModule),
        typeof(AbpDistributedLockingModule)
        )]
    public class SharpFortCasbinRbacDomainModule : AbpModule
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
                    var redisConfig = configuration["Redis:Configuration"]
                        ?? throw new InvalidOperationException("Redis:Configuration \u914d\u7f6e\u4e0d\u80fd\u4e3a\u7a7a\u3002\u8bf7\u5728 appsettings.json \u4e2d\u914d\u7f6e Redis \u8fde\u63a5\u5b57\u7b26\u4e32\u3002");
                    var connection = ConnectionMultiplexer.Connect(redisConfig);
                    return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
                });
            }

        }
    }
}
