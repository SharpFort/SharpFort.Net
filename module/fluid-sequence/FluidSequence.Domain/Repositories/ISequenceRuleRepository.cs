using System.Threading.Tasks;
using FluidSequence.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace FluidSequence.Domain.Repositories
{
    public interface ISequenceRuleRepository : ISqlSugarRepository<SysSequenceRule, Guid>
    {
        /// <summary>
        /// Hi-Lo 原子号段推进：
        /// 以单条原子 SQL 将指定规则的 CurrentValue 向上推进 <paramref name="count"/> 步，
        /// 并返回推进前的起始值（包含）和推进后的结束值（包含），供上层填充缓冲队列。
        /// </summary>
        /// <param name="ruleCode">规则编码</param>
        /// <param name="count">本次预取的号码数量（BufferCount）</param>
        /// <returns>
        ///   (rangeStart, rangeEnd): 调用方可由此枚举 [rangeStart, rangeEnd] 的整数序列。
        ///   若规则不存在则抛出 UserFriendlyException。
        /// </returns>
        Task<(int rangeStart, int rangeEnd)> AtomicAdvanceAsync(string ruleCode, int count);
    }
}
