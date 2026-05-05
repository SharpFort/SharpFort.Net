using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace SharpFort.Core.Modularity;

/// <summary>
/// Sf框架模块管理器
/// </summary>
[Dependency(ReplaceServices = true)]
public partial class SfModuleManager : ModuleManager, IModuleManager, ISingletonDependency
{
    private readonly IModuleContainer _moduleContainer;
    private readonly IEnumerable<IModuleLifecycleContributor> _lifecycleContributors;
    private readonly ILogger<SfModuleManager> _logger;

    /// <summary>
    /// 初始化模块管理器
    /// </summary>
    public SfModuleManager(
        IModuleContainer moduleContainer,
        ILogger<SfModuleManager> logger,
        IOptions<AbpModuleLifecycleOptions> options,
        IServiceProvider serviceProvider) 
        : base(moduleContainer, logger, options, serviceProvider)
    {
        _moduleContainer = moduleContainer;
        _logger = logger;
        _lifecycleContributors = options.Value.Contributors
            .Select(serviceProvider.GetRequiredService)
            .Cast<IModuleLifecycleContributor>()
            .ToArray();
    }

    /// <summary>
    /// 初始化所有模块
    /// </summary>
    /// <param name="context">应用程序初始化上下文</param>
    public override async Task InitializeModulesAsync(ApplicationInitializationContext context)
    {
        LogModuleInitStart();

        var moduleCount = 0;
        var stopwatch = new Stopwatch();
        var totalTime = 0L;

        foreach (var contributor in _lifecycleContributors)
        {
            foreach (var module in _moduleContainer.Modules)
            {
                try
                {
                    stopwatch.Restart();
                    await contributor.InitializeAsync(context, module.Instance);
                    stopwatch.Stop();

                    totalTime += stopwatch.ElapsedMilliseconds;
                    moduleCount++;

                    // 仅记录耗时超过1ms的模块
                    if (stopwatch.ElapsedMilliseconds > 1 && _logger.IsEnabled(LogLevel.Debug))
                    {
                        var moduleName = module.Assembly.GetName().Name ?? "Unknown";
                        LogModuleLoaded(stopwatch.ElapsedMilliseconds, moduleName);
                    }
                }
                catch (Exception ex)
                {
                    throw new AbpInitializationException(
                        $"模块 {module.Type.AssemblyQualifiedName} 在 {contributor.GetType().FullName} 阶段初始化失败: {ex.Message}",
                        ex);
                }
            }
        }

        LogModuleInitComplete(moduleCount, totalTime);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "==========模块Initialize初始化统计-跳过0ms模块==========")]
    private partial void LogModuleInitStart();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "耗时-{Time}ms,已加载模块-{ModuleName}")]
    private partial void LogModuleLoaded(long time, string moduleName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "==========【{Count}】个模块初始化执行完毕，总耗时【{Time}ms】==========")]
    private partial void LogModuleInitComplete(int count, long time);
}
