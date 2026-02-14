using Yi.Framework.Ddd.Application.Contracts;

namespace FluidSequence.Application.Contracts.Dtos
{
    public class SequenceRuleGetListInput : PagedAllResultRequestDto
    {
        public string RuleName { get; set; }
        public string RuleCode { get; set; }
    }
}
