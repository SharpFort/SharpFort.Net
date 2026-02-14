using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using FluidSequence.Domain.Entities;
using FluidSequence.Domain.Repositories;
using FluidSequence.Domain.Services.Strategies;
using SqlSugar;

namespace FluidSequence.Domain.Services
{
    public class SequenceDomainService : DomainService
    {
        private readonly ISequenceRuleRepository _repository;
        private readonly IEnumerable<IPlaceholderStrategy> _strategies;

        public SequenceDomainService(ISequenceRuleRepository repository, IEnumerable<IPlaceholderStrategy> strategies)
        {
            _repository = repository;
            _strategies = strategies;
        }

        public async Task<string> GenerateNextAsync(string ruleCode, Dictionary<string, string> context = null)
        {
             // Optimistic Lock Retry
             int retry = 5;
             while (retry-- > 0)
             {
                 var rule = await _repository.GetAsync(r => r.RuleCode == ruleCode);
                 if (rule == null) throw new UserFriendlyException($"Rule {ruleCode} not found");

                 if (!rule.TryReset(DateTime.Now))
                 {
                      rule.NextValue();
                 }

                 try 
                 {
                      await _repository.UpdateAsync(rule);
                      return ParseTemplate(rule, context);
                 }
                 catch (Exception) // Catching generic exception as SqlSugar might throw various exceptions for concurrency
                 {
                      if (retry == 0) throw;
                      await Task.Delay(20);
                 }
             }
             throw new UserFriendlyException("Concurrent update failed");
        }

        public string TestGenerate(SysSequenceRule rule, Dictionary<string, string> context)
        {
             return ParseTemplate(rule, context);
        }

        private string ParseTemplate(SysSequenceRule rule, Dictionary<string, string> context)
        {
            return Regex.Replace(rule.Template, @"\{(.*?)\}", match => 
            {
                string key = match.Groups[1].Value;
                foreach (var strategy in _strategies)
                {
                    if (strategy.CanHandle(key))
                    {
                        return strategy.Handle(key, rule, context);
                    }
                }
                // Fallback to context
                if (context != null && context.ContainsKey(key)) return context[key];
                
                return match.Value;
            });
        }
    }
}
