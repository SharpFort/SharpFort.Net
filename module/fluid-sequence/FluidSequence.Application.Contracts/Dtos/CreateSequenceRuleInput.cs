using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluidSequence.Domain.Shared.Enums;

namespace FluidSequence.Application.Contracts.Dtos
{
    public class CreateSequenceRuleInput
    {
        [Required]
        [MaxLength(50)]
        public string RuleName { get; set; }

        [Required]
        [MaxLength(50)]
        public string RuleCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string Template { get; set; }

        public int Step { get; set; } = 1;
        public int SeqLength { get; set; } = 6;
        public int MinValue { get; set; } = 1;
        public int MaxValue { get; set; } = 999999999;
        public SequenceResetType ResetType { get; set; } = SequenceResetType.None;
        /// <summary>
        /// 备注（可选）
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 扩展配置（可选 JSON 键值对）
        /// 支持以下运行时配置键：
        ///   - EnableBuffer (bool)  : 是否启用 Hi-Lo 预取缓冲，默认 false
        ///   - BufferCount (int)    : 单次预取号段大小，默认 50
        ///   - FiscalYearStartMonth (int) : 财年起始月份，配合 {FY} 占位符使用，默认 1
        /// 示例: { "EnableBuffer": true, "BufferCount": 100 }
        /// </summary>
        public Dictionary<string, object>? ExtensionProps { get; set; }
    }
}
