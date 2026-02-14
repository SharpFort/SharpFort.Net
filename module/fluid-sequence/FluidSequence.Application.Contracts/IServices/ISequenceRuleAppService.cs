using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using FluidSequence.Application.Contracts.Dtos;

namespace FluidSequence.Application.Contracts.IServices
{
    public interface ISequenceRuleAppService : ICrudAppService< 
        SequenceRuleDto, 
        long, 
        SequenceRuleGetListInput, 
        CreateSequenceRuleInput, 
        UpdateSequenceRuleInput>
    {
        Task<string> TestGenerateAsync(string ruleCode, Dictionary<string, string> context);
        Task<List<PlaceholderMetaDto>> GetPlaceholdersAsync();
    }
}
