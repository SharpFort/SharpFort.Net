using Sf.Abp.Application;
using Sf.Abp.SqlsugarCore;
using Volo.Abp.Autofac;

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
