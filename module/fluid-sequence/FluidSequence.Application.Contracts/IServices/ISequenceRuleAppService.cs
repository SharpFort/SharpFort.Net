using Volo.Abp.Application.Services;
using SharpFort.FluidSequence.Application.Contracts.Dtos;

namespace SharpFort.FluidSequence.Application.Contracts.IServices
{
    public interface ISequenceRuleAppService : ICrudAppService<
        SequenceRuleDto,
        Guid,
        SequenceRuleGetListInput,
        CreateSequenceRuleInput,
        UpdateSequenceRuleInput>
    {
        Task<string> TestGenerateAsync(string ruleCode, Dictionary<string, string> context);
        Task<List<PlaceholderMetaDto>> GetPlaceholdersAsync();
    }
}
