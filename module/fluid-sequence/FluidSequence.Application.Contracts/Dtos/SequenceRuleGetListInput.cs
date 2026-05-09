using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.FluidSequence.Application.Contracts.Dtos
{
    public class SequenceRuleGetListInput : PagedAllResultRequestDto
    {
        public string? RuleName { get; set; }
        public string? RuleCode { get; set; }
    }
}
