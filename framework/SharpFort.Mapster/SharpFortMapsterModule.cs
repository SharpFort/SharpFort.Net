using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using SharpFort.Core;

namespace SharpFort.Mapster
{
    /// <summary>
    /// Sf框架Mapster模块
    /// 用于配置和注册Mapster相关服务
    /// </summary>
    [DependsOn(
        typeof(SharpFortCoreModule),
        typeof(AbpObjectMappingModule)
    )]
    public class SharpFortMapsterModule : AbpModule
    {
        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="context">服务配置上下文</param>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            // 注册Mapster相关服务
            services.AddTransient<IAutoObjectMappingProvider, MapsterAutoObjectMappingProvider>();
            services.AddTransient<IObjectMapper, MapsterObjectMapper>();
        }
    }
}