using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;
using SqlSugar;

namespace Yi.Framework.Ai.SqlSugarCore
{
    [ConnectionStringName("Ai")]
    public class AiModuleDbContext : SqlSugarDbContext
    {
        public AiModuleDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }
    }
}
