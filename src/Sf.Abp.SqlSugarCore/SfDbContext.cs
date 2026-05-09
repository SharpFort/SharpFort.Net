using Volo.Abp.DependencyInjection;

using SharpFort.SqlSugarCore;

namespace Sf.Abp.SqlSugarCore
{
    public class SfDbContext(IAbpLazyServiceProvider lazyServiceProvider) : SqlSugarDbContext(lazyServiceProvider)
    {
    }
}
