using Volo.Abp.Modularity;

namespace SharpFort.Core
{
    /// <summary>
    /// Sf框架核心模块
    /// </summary>
    /// <remarks>
    /// 提供框架的基础功能和核心服务
    /// </remarks>
    public class SharpFortCoreModule : AbpModule
    {
        /// <summary>
        /// 配置服务
        /// </summary>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }

        /// <summary>
        /// 应用程序初始化
        /// </summary>
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);
        }
    }
}