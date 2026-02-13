using Volo.Abp.DependencyInjection;
using Yi.Framework.SqlSugarCore;

namespace Yi.Framework.FileManagement.SqlSugarCore
{
    public class FileManagementDbContext : SqlSugarDbContext
    {
        public FileManagementDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }
    }
}
