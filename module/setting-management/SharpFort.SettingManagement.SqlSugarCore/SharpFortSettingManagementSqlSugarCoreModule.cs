using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using SharpFort.SettingManagement.Domain;
using SharpFort.SqlSugarCore;

namespace SharpFort.SettingManagement.SqlSugarCore
{
    [DependsOn(
        typeof(SharpFortSettingManagementDomainModule),
        typeof(SharpFortSqlSugarCoreModule)
        )]
    public class SharpFortSettingManagementSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<ISettingRepository, SqlSugarCoreSettingRepository>();
        }
    }
}
