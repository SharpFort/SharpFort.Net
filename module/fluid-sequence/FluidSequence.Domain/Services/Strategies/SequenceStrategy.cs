using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using FluidSequence.Domain.Entities;

namespace FluidSequence.Domain.Services.Strategies
{
    public class SequenceStrategy : IPlaceholderStrategy, ISingletonDependency
    {
        public bool CanHandle(string key)
        {
            return key == "SEQ" || key == "SEQ36";
        }

        public string Handle(string key, SysSequenceRule rule, Dictionary<string, string> context)
        {
             if (key == "SEQ") return rule.CurrentValue.ToString().PadLeft(rule.SeqLength, '0');
             if (key == "SEQ36") return ConvertToBase36(rule.CurrentValue); 
             return key;
        }

        private string ConvertToBase36(long value)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (value == 0) return "0";
            string result = "";
            while (value > 0)
            {
                result = chars[(int)(value % 36)] + result;
                value /= 36;
            }
            return result;
        }
    }
}
