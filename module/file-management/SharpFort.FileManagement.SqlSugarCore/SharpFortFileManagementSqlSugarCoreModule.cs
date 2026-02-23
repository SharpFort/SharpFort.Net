using SharpFort.FileManagement.Domain;
using SharpFort.Mapster;
using SharpFort.SqlSugarCore;

namespace SharpFort.FileManagement.SqlSugarCore
{
    [DependsOn(
        typeof(SharpFortFileManagementDomainModule),
        typeof(SharpFortMapsterModule),
        typeof(SharpFortSqlSugarCoreModule)
    )]
    public class SharpFortFileManagementSqlSugarCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSfDbContext<FileManagementDbContext>();
        }
    }
}