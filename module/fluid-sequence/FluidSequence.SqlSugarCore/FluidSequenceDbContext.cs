using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using SqlSugar;
using SharpFort.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.FluidSequence.Domain.Shared.Consts;

namespace FluidSequence.SqlSugarCore
{
    [ConnectionStringName(FluidSequenceConsts.ConnectionStringName)]
    public class FluidSequenceDbContext(IAbpLazyServiceProvider lazyServiceProvider) : SqlSugarDbContext(lazyServiceProvider), ISqlSugarDbContext
    {
        public new ISqlSugarClient SqlSugarClient => base.SqlSugarClient;

        public void BackupDataBase()
        {
            throw new NotImplementedException();
        }
    }
}
