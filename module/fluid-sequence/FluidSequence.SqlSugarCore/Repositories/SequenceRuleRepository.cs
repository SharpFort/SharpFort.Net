using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;
using FluidSequence.Domain.Entities;
using FluidSequence.Domain.Repositories;

namespace FluidSequence.SqlSugarCore.Repositories
{
    public class SequenceRuleRepository : SqlSugarRepository<SysSequenceRule, long>, ISequenceRuleRepository, ITransientDependency
    {
        public SequenceRuleRepository(ISugarDbContextProvider<ISqlSugarDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
