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
        public long MinValue { get; set; } = 1;
        public long MaxValue { get; set; } = 999999999;
        public SequenceResetType ResetType { get; set; } = SequenceResetType.None;
        /// <summary>
        /// 备注（选填）
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 扩展配置属性（选填，JSON 键值对）
        /// 用于存储规则的可选配置，例如：
        ///   { "EnableBuffer": true, "BufferCount": 100 }  → 开启 Hi-Lo 预取
        ///   { "FiscalYearStartMonth": 4 }               → 财年起始月
        /// </summary>
        public Dictionary<string, object>? ExtensionProps { get; set; }
    }
}
