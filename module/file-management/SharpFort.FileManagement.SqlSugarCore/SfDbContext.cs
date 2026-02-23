using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;

namespace SharpFort.FileManagement.SqlSugarCore
{
    public class FileManagementDbContext : SqlSugarDbContext
    {
        public FileManagementDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }
    }
}
