using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using FluidSequence.Domain.Entities;
using FluidSequence.Domain.Repositories;
using FluidSequence.Domain.Services.Strategies;
using SqlSugar;

namespace FluidSequence.Domain.Services
{
    /// <summary>
    /// 流水号核心领域服务，负责生成下一个流水号。
    ///
    /// 支持两种取号模式（通过规则的 ExtensionProps 配置）：
    ///
    ///   [普通模式] EnableBuffer = false（默认）
    ///     - 每次调用都访问数据库，用乐观锁（版本号）保证并发安全。
    ///     - 适合并发较低、要求号码尽量连续的场景。
    ///
    ///   [Hi-Lo 缓冲模式] EnableBuffer = true
    ///     - 由 SequenceHiLoBufferService 在内存中维护号码队列，
    ///       队列耗尽时才去 DB 原子推进一批（BufferCount 个），
    ///       数据库写入次数降低 BufferCount 倍，适合高并发场景。
    ///     - 代价：应用重启会丢弃缓冲队列中剩余号码，号码序列可能出现空洞，但保证全局唯一递增。
    /// </summary>
    public class SequenceDomainService : DomainService
    {
        private readonly ISequenceRuleRepository _repository;
        private readonly IEnumerable<IPlaceholderStrategy> _strategies;
        private readonly SequenceHiLoBufferService _hiLoBuffer;

        public SequenceDomainService(
            ISequenceRuleRepository repository,
            IEnumerable<IPlaceholderStrategy> strategies,
            SequenceHiLoBufferService hiLoBuffer)
        {
            _repository  = repository;
            _strategies  = strategies;
            _hiLoBuffer  = hiLoBuffer;
        }

        /// <summary>
        /// 生成指定规则的下一个流水号（主入口）。
        /// </summary>
        /// <param name="ruleCode">规则编码（唯一业务标识）</param>
        /// <param name="context">上下文参数，支持 {UserCode}/{DeptCode}/{Param:XXX} 等占位符</param>
        public async Task<string> GenerateNextAsync(string ruleCode, Dictionary<string, string> context = null)
        {
            // 查询规则，获取配置（仅用于读 ExtensionProps，不在此做状态修改）
            var rule = await _repository.GetAsync(r => r.RuleCode == ruleCode)
                       ?? throw new UserFriendlyException($"流水号规则 [{ruleCode}] 不存在。");

            // ── 路由判断：是否启用 Hi-Lo 缓冲模式 ──────────────────────────────────
            bool enableBuffer = rule.ExtensionProps != null
                && rule.ExtensionProps.TryGetValue("EnableBuffer", out var eb)
                && Convert.ToBoolean(eb);

            if (enableBuffer)
            {
                // Hi-Lo 模式：从内存队列取号，队列空时原子批量预取
                int bufferCount = 50; // 默认单次预取 50 个
                if (rule.ExtensionProps!.TryGetValue("BufferCount", out var bc))
                    bufferCount = Math.Max(1, Convert.ToInt32(bc));

                // 从缓冲队列取到的是最新的 CurrentValue，直接用于模板渲染
                // 注意：此时 rule.CurrentValue 是旧值，需要临时覆盖以便 ParseTemplate 正确使用
                // NextAsync 返回 int（与 CurrentValue 类型一致），SetCurrentValue 接受 int
                int bufferedValue = await _hiLoBuffer.NextAsync(ruleCode, bufferCount);
                rule.SetCurrentValue(bufferedValue); // 见 SysSequenceRule 中的辅助方法
                return ParseTemplate(rule, context);
            }

            // ── 普通模式：乐观锁重试 ──────────────────────────────────────────────
            return await GenerateWithOptimisticLockAsync(ruleCode, context);
        }

        /// <summary>
        /// 普通模式：乐观锁（版本号）+指数退避 重试取号。
        ///
        /// 【修复：问题1 — 乐观锁机制不完整】
        ///   原代码依赖 catch(Exception) 来识别版本冲突，但 SqlSugar 的
        ///   IsEnableUpdateVersionValidation 在版本不匹配时返回影响行数 0 而非抛出异常，
        ///   导致重试逻辑实际上从未被触发，高并发时会发生号码重复。
        ///   修复方案：改为检查 UpdateAsync 的返回值（bool），false 表示版本冲突，
        ///   触发重试并加入指数退避抖动，避免多个并发请求同频率碰撞。
        ///
        /// 【修复：问题2 — TryReset 后 NextValue 缺失】
        ///   原代码：if (!TryReset) { NextValue(); }
        ///   问题：触发重置时，CurrentValue 被重置为 MinValue（如 1），直接用 1 生成号码，
        ///         下一个请求才从 MinValue + Step 开始，导致每个新周期第一个号与初始化逻辑不一致。
        ///         若 MinValue != 1 或 Step != 1，两段逻辑会产生不同起始值。
        ///   修复：重置后同样调用 NextValue()，确保无论是否跨周期，"推进后再生成"的语义始终一致：
        ///         初始化 (CurrentValue=0) → NextValue → 生成 Step（如 1）
        ///         重置后  (CurrentValue=MinValue) → NextValue → 生成 MinValue + Step（如 2）
        ///
        ///   注意：如果业务期望每个新周期第一个号就是 MinValue，
        ///         应将 SysSequenceRule 的 CurrentValue 初始值设为 MinValue - Step，而非 0。
        /// </summary>
        private async Task<string> GenerateWithOptimisticLockAsync(
            string ruleCode,
            Dictionary<string, string> context)
        {
            const int maxRetry = 8;

            for (int attempt = 0; attempt < maxRetry; attempt++)
            {
                // 每次循环都重新从 DB 读取，获取最新版本号，避免拿到脏数据
                var rule = await _repository.GetAsync(r => r.RuleCode == ruleCode)
                           ?? throw new UserFriendlyException($"流水号规则 [{ruleCode}] 不存在。");

                // ── 问题2修复：重置与递增统一为"先重置（若需要），再递增"──
                rule.TryReset(DateTime.Now); // 无论是否发生重置，都执行 NextValue
                rule.NextValue();            // CurrentValue += Step（溢出则抛异常）

                // 乐观锁更新：SqlSugar 会在 WHERE 条件中附加版本号校验
                // 若版本已被其他请求修改，UpdateAsync 返回 false（影响行数=0）
                bool updated = await _repository.UpdateAsync(rule);

                if (updated)
                {
                    // ── 问题1修复：明确检查返回值，更新成功才渲染并返回 ──
                    return ParseTemplate(rule, context);
                }

                // 版本冲突，指数退避后重试
                // 抖动范围：[baseMs, baseMs * 2)，避免多个请求在同一时刻重试（惊群）
                int baseMs = 10 * (1 << attempt); // 10, 20, 40, 80, 160, ...ms
                await Task.Delay(baseMs + Random.Shared.Next(baseMs));
            }

            throw new UserFriendlyException(
                $"流水号规则 [{ruleCode}] 在 {maxRetry} 次重试后仍发生并发冲突，请降低并发压力或启用 Hi-Lo 缓冲模式。");
        }

        /// <summary>
        /// 测试模板渲染（不写库，仅用于前端预览效果）。
        /// </summary>
        public string TestGenerate(SysSequenceRule rule, Dictionary<string, string> context)
        {
            return ParseTemplate(rule, context);
        }

        /// <summary>
        /// 将模板字符串中的占位符替换为实际值。
        /// 占位符格式：{KEY} 或 {KEY:PARAM:...}
        /// 替换顺序：Strategy链 → 上下文字典 → 原样保留
        /// </summary>
        private string ParseTemplate(SysSequenceRule rule, Dictionary<string, string> context)
        {
            return Regex.Replace(rule.Template, @"\{(.*?)\}", match =>
            {
                string key = match.Groups[1].Value;

                // 优先由策略链处理（时间、序列号、随机、上下文）
                foreach (var strategy in _strategies)
                {
                    if (strategy.CanHandle(key))
                        return strategy.Handle(key, rule, context);
                }

                // 策略链未命中时，尝试从上下文字典直接读取
                if (context != null && context.TryGetValue(key, out var ctxVal))
                    return ctxVal;

                // 无法解析的占位符原样保留，方便调试
                return match.Value;
            });
        }
    }
}
