using Yi.Framework.FileManagement.Domain;
using Yi.Framework.Mapster;
using Yi.Framework.SqlSugarCore;

namespace Yi.Framework.FileManagement.SqlSugarCore
{
    [DependsOn(
        typeof(YiFrameworkFileManagementDomainModule),
        typeof(YiFrameworkMapsterModule),
        typeof(YiFrameworkSqlSugarCoreModule)
    )]
    public class YiFrameworkFileManagementSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddYiDbContext<FileManagementDbContext>();
        }
    }
}