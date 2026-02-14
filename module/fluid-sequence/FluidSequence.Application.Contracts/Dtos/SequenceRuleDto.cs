using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using FluidSequence.Domain.Shared.Enums;

namespace FluidSequence.Application.Contracts.Dtos
{
    public class SequenceRuleDto : AuditedEntityDto<long>
    {
        public string RuleName { get; set; }
        public string RuleCode { get; set; }
        public string Template { get; set; }
        public long CurrentValue { get; set; }
        public int Step { get; set; }
        public int SeqLength { get; set; }
        public long MinValue { get; set; }
        public long MaxValue { get; set; }
        public SequenceResetType ResetType { get; set; }
        public DateTime? LastResetTime { get; set; }
        public string Remark { get; set; }
        public Dictionary<string, object> ExtensionProps { get; set; }
    }
}
