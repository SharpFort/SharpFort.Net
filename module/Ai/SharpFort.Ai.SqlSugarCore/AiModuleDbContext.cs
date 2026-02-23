using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;
using SqlSugar;

namespace SharpFort.Ai.SqlSugarCore
{
    [ConnectionStringName("Ai")]
    public class AiModuleDbContext : SqlSugarDbContext
    {
        public AiModuleDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }
    }
}
