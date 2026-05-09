using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;

namespace SharpFort.FileManagement.SqlSugarCore
{
    public class FileManagementDbContext(IAbpLazyServiceProvider lazyServiceProvider) : SqlSugarDbContext(lazyServiceProvider)
    {
    }
}
