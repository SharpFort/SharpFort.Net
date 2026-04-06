using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using FluidSequence.Domain.Repositories;

namespace FluidSequence.Domain.Services
{
    /// <summary>
    /// Hi-Lo 序列号缓冲服务（单例）。
    ///
    /// 设计目标：
    ///   对于启用了 EnableBuffer=true 的规则，让每个应用实例在内存中维护一个号码池（ConcurrentQueue）。
    ///   当号码池耗尽时，才去数据库一次性推进 BufferCount 步，并将这批号码填入队列。
    ///   这样可以将数据库写操作降低 BufferCount 倍，大幅提升高并发吞吐量。
    ///
    /// 并发安全策略：
    ///   每个 ruleCode 独享一把 SemaphoreSlim(1,1)，保证同一时刻只有一个线程去 DB 预取号段，
    ///   其余线程在锁外短暂自旋等待后重新检查队列，避免重复数据库调用（惊群效应）。
    ///
    /// 适用场景：
    ///   - 高并发、写多读少的流水号场景（如：订单号、出库单号）
    ///   - 允许单实例重启后丢失尾部预取但未使用的号码段（号码不连续，但唯一且单调递增）
    ///
    /// 不适用场景：
    ///   - 要求号码严格连续（不允许空洞）的场景，应关闭 EnableBuffer
    ///   - 多实例部署时如需跨实例共享缓冲队列，应替换为 Redis Stream 实现
    /// </summary>
    public class SequenceHiLoBufferService : ISingletonDependency
    {
        // 每个 ruleCode → 待使用的号码队列
        private readonly ConcurrentDictionary<string, ConcurrentQueue<int>> _buffers
            = new ConcurrentDictionary<string, ConcurrentQueue<int>>();

        // 每个 ruleCode → 用于控制"只有一个线程去 DB 预取"的信号量
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks
            = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly ISequenceRuleRepository _repository;

        public SequenceHiLoBufferService(ISequenceRuleRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 从缓冲队列中取出下一个序列号。
        /// 若队列为空，则去数据库原子地预取一批号码后再出队。
        /// </summary>
        /// <param name="ruleCode">规则编码</param>
        /// <param name="bufferCount">单次预取数量（来自规则的 ExtensionProps.BufferCount）</param>
        public async Task<int> NextAsync(string ruleCode, int bufferCount)
        {
            var queue = _buffers.GetOrAdd(ruleCode, _ => new ConcurrentQueue<int>());
            var sem   = _locks.GetOrAdd(ruleCode, _ => new SemaphoreSlim(1, 1));

            // 快路径：队列非空，直接出队，无需加锁，零竞争
            if (queue.TryDequeue(out var value))
                return value;

            // 慢路径：队列已耗尽，加锁去 DB 预取
            await sem.WaitAsync();
            try
            {
                // 二次检查：有可能另一个线程刚刚填充好了队列
                if (queue.TryDequeue(out value))
                    return value;

                // 原子推进：将 DB 中 current_value 向上推进 bufferCount 步
                // rangeStart 是本批次的第一个号，rangeEnd 是最后一个号（均含）
                var (rangeStart, rangeEnd) = await _repository.AtomicAdvanceAsync(ruleCode, bufferCount);

                // 将 [rangeStart+1, rangeEnd] 批量入队（rangeStart 本次直接返回，不入队）
                // 注意：AtomicAdvance 返回的 rangeStart = 推进前的 current_value
                //       即数据库原本已记录到 rangeStart，[rangeStart+1, rangeEnd] 是新增的号段
                for (var i = rangeStart + 1; i <= rangeEnd; i++)
                    queue.Enqueue(i);

                // 返回号段的第一个号
                return rangeStart + 1;
            }
            finally
            {
                sem.Release();
            }
        }

        /// <summary>
        /// 清除指定规则的内存缓冲（用于规则被修改/删除后主动失效）。
        /// </summary>
        public void Invalidate(string ruleCode)
        {
            _buffers.TryRemove(ruleCode, out _);
            // 注意：不移除 _locks，让信号量继续存在避免空指针
        }
    }
}
