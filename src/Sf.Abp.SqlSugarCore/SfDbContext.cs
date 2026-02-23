using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.DependencyInjection;

using SharpFort.SqlSugarCore;

namespace Sf.Abp.SqlSugarCore
{
    public class SfDbContext : SqlSugarDbContext
    {
        public SfDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }
    }
}
