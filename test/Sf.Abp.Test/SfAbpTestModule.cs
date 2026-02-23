using Hangfire;
using Hangfire.MemoryStorage;
using StackExchange.Redis;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.BackgroundWorkers;
using SharpFort.Application;
using SharpFort.SqlsugarCore;

namespace Sf.Abp.Test
{
    [DependsOn(
        typeof(SfAbpSqlSugarCoreModule),
        typeof(SfAbpApplicationModule),
        
        typeof(AbpAutofacModule)
        )]
    public class SfAbpTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
