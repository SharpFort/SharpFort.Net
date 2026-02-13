using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Application.Dtos;
using SqlSugar;
using Yi.Framework.Ddd.Application;
using FluidSequence.Application.Contracts.Dtos;
using FluidSequence.Application.Contracts.IServices;
using FluidSequence.Domain.Entities;
using FluidSequence.Domain.Services;
using FluidSequence.Domain.Repositories;

namespace FluidSequence.Application.Services
{
    [Authorize]
    public class SequenceRuleAppService : YiCrudAppService< 
        SysSequenceRule, 
        SequenceRuleDto, 
        long, 
        SequenceRuleGetListInput, 
        CreateSequenceRuleInput, 
        UpdateSequenceRuleInput>,
        ISequenceRuleAppService
    {
        private readonly SequenceDomainService _domainService;
        private readonly ISequenceRuleRepository _repository;

        public SequenceRuleAppService(
            ISequenceRuleRepository repository, 
            SequenceDomainService domainService) 
            : base(repository)
        {
            _repository = repository;
            _domainService = domainService;
        }

        public async Task<string> TestGenerateAsync(string ruleCode, Dictionary<string, string> context)
        {
             var rule = await _repository.GetAsync(r => r.RuleCode == ruleCode);
             if (rule == null) throw new Volo.Abp.UserFriendlyException($"Rule {ruleCode} not found");

             return _domainService.TestGenerate(rule, context);
        }

        public Task<List<PlaceholderMetaDto>> GetPlaceholdersAsync()
        {
            var dtos = new List<PlaceholderMetaDto>();
            foreach (var meta in PlaceholderRegistry.Definitions)
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
            
            var entities = await _repository._DbQueryable
                .WhereIF(!string.IsNullOrWhiteSpace(input.RuleName), x => x.RuleName.Contains(input.RuleName))
                .WhereIF(!string.IsNullOrWhiteSpace(input.RuleCode), x => x.RuleCode.Contains(input.RuleCode))
                .ToPageListAsync(input.SkipCount / input.MaxResultCount + 1, input.MaxResultCount, total);

            return new PagedResultDto<SequenceRuleDto>(total, ObjectMapper.Map<List<SysSequenceRule>, List<SequenceRuleDto>>(entities));
        }
    }
}
