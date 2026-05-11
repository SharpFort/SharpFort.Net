using Volo.Abp.Application.Dtos;
using SharpFort.FluidSequence.Domain.Shared.Enums;

namespace SharpFort.FluidSequence.Application.Contracts.Dtos
{
    public class SequenceRuleDto : AuditedEntityDto<Guid>
    {
        public string RuleName { get; set; } = null!;
        public string RuleCode { get; set; } = null!;
        public string Template { get; set; } = null!;
        public int CurrentValue { get; set; }
        public int Step { get; set; }
        public int SeqLength { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public SequenceResetType ResetType { get; set; }
        public DateTime? LastResetTime { get; set; }
        public string Remark { get; set; } = null!;
        public Dictionary<string, object> ExtensionProps { get; set; } = null!;
    }
}
