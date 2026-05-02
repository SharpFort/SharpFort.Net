#nullable disable
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using SharpFort.SqlSugarCore;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;
using FluidSequence.Domain.Entities;
using FluidSequence.Domain.Repositories;

namespace FluidSequence.SqlSugarCore.Repositories
{
    public class SequenceRuleRepository : SqlSugarRepository<SysSequenceRule, Guid>, ISequenceRuleRepository, ITransientDependency
    {
        public SequenceRuleRepository(ISugarDbContextProvider<ISqlSugarDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        /// <summary>
        /// Hi-Lo 原子号段推进实现。
        ///
        /// 核心思路：用一条 UPDATE...RETURNING SQL 原子地完成"取值+推进"两步：
        ///   UPDATE sys_sequence_rule
        ///   SET    current_value = current_value + @count, version = version + 1
        ///   WHERE  rule_code = @ruleCode
        ///   RETURNING current_value - @count AS range_start, current_value AS range_end
        ///
        /// 为什么必须用原子 SQL 而不能先查后改？
        ///   若并发 10 个请求同时读到 current_value=100，它们都会认为自己拿到了 [101,150] 的号段，
        ///   最终 10 个请求返回完全相同的号码，发生严重重复。
        ///   原子 SQL 由数据库序列化执行，每次推进彼此不重叠，完全消除了并发窗口。
        /// </summary>
        public async Task<(int rangeStart, int rangeEnd)> AtomicAdvanceAsync(string ruleCode, int count)
        {
            var db = await GetDbContextAsync();

            // 使用 PostgreSQL 原子 UPDATE...RETURNING
            // current_value 推进 count 步后返回新值（rangeEnd），再减 count 得到旧值（rangeStart）
            var sql = @"
                UPDATE sys_sequence_rule
                SET    current_value = current_value + @count,
                       version       = version + 1
                WHERE  rule_code = @ruleCode
                RETURNING
                    current_value - @count AS range_start,
                    current_value          AS range_end";

            var dt = await db.Ado.GetDataTableAsync(sql, new
            {
                count    = count,
                ruleCode = ruleCode
            });

            if (dt.Rows.Count == 0)
                throw new UserFriendlyException($"流水号规则 [{ruleCode}] 不存在，无法执行号段预取。");

            var rangeStart = Convert.ToInt32(dt.Rows[0]["range_start"]);
            var rangeEnd   = Convert.ToInt32(dt.Rows[0]["range_end"]);

            return (rangeStart, rangeEnd);
        }
    }
}
