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
        public string Remark { get; set; }
        public Dictionary<string, object> ExtensionProps { get; set; }
    }
}
