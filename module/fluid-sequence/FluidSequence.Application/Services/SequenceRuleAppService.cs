using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using SqlSugar;
using SharpFort.Ddd.Application;
using SharpFort.FluidSequence.Application.Contracts.Dtos;
using SharpFort.FluidSequence.Application.Contracts.IServices;
using SharpFort.FluidSequence.Domain.Entities;
using SharpFort.FluidSequence.Domain.Services;
using SharpFort.FluidSequence.Domain.Repositories;

namespace SharpFort.FluidSequence.Application.Services
{
    [Authorize]
    public class SequenceRuleAppService(
        ISequenceRuleRepository repository,
        SequenceDomainService domainService) : SfCrudAppService<
        SysSequenceRule,
        SequenceRuleDto,
        Guid,
        SequenceRuleGetListInput,
        CreateSequenceRuleInput,
        UpdateSequenceRuleInput>(repository),
        ISequenceRuleAppService
    {
        private readonly SequenceDomainService _domainService = domainService;
        private readonly ISequenceRuleRepository _repository = repository;

        public async Task<string> TestGenerateAsync(string ruleCode, Dictionary<string, string> context)
        {
            SysSequenceRule rule = await _repository.GetAsync(r => r.RuleCode == ruleCode);
            return rule == null
                ? throw new Volo.Abp.UserFriendlyException($"Rule {ruleCode} not found")
                : _domainService.TestGenerate(rule, context);
        }

        public Task<List<PlaceholderMetaDto>> GetPlaceholdersAsync()
        {
            List<PlaceholderMetaDto> dtos = [];
            foreach (PlaceholderMeta meta in PlaceholderRegistry.Definitions)
            {
                dtos.Add(new PlaceholderMetaDto
                {
                    Key = meta.Key,
                    Label = meta.Label,
                    Group = meta.Group
                });
            }
            return Task.FromResult(dtos);
        }

        public override async Task<PagedResultDto<SequenceRuleDto>> GetListAsync(SequenceRuleGetListInput input)
        {
            RefAsync<int> total = 0;

            List<SysSequenceRule> entities = await _repository._DbQueryable
                .WhereIF(!string.IsNullOrWhiteSpace(input.RuleName), x => x.RuleName.Contains(input.RuleName!))
                .WhereIF(!string.IsNullOrWhiteSpace(input.RuleCode), x => x.RuleCode.Contains(input.RuleCode!))
                .ToPageListAsync(input.SkipCount / input.MaxResultCount + 1, input.MaxResultCount, total);

            return new PagedResultDto<SequenceRuleDto>(total, ObjectMapper.Map<List<SysSequenceRule>, List<SequenceRuleDto>>(entities));
        }
    }
}
