using FluidSequence.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace FluidSequence.Domain.Repositories
{
    public interface ISequenceRuleRepository : ISqlSugarRepository<SysSequenceRule, long>
    {
    }
}
