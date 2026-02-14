using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Yi.Framework.SqlSugarCore;
using Yi.Framework.SqlSugarCore.Abstractions;
using Yi.Framework.SqlSugarCore.Repositories;
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
