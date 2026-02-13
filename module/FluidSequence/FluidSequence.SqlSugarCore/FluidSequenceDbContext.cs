using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using SqlSugar;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.SqlSugarCore.Abstractions;
using FluidSequence.Domain.Shared.Consts;
using FluidSequence.Domain.Entities;

namespace FluidSequence.SqlSugarCore
{
    [ConnectionStringName(FluidSequenceConsts.ConnectionStringName)]
    public class FluidSequenceDbContext : SqlSugarDbContext, ISqlSugarDbContext
    {
        public FluidSequenceDbContext(IAbpLazyServiceProvider lazyServiceProvider) : base(lazyServiceProvider)
        {
        }

        public new ISqlSugarClient SqlSugarClient => base.SqlSugarClient;

        public void BackupDataBase()
        {
            throw new NotImplementedException();
        }
    }
}
