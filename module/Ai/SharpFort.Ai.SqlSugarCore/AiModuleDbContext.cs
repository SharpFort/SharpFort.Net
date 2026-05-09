using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;

namespace SharpFort.Ai.SqlSugarCore
{
    [ConnectionStringName("Ai")]
    public class AiModuleDbContext(IAbpLazyServiceProvider lazyServiceProvider) : SqlSugarDbContext(lazyServiceProvider)
    {
    }
}
