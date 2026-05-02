using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using FluidSequence.Domain.Entities;

namespace FluidSequence.Domain.Services.Strategies
{
    public class SequenceStrategy : IPlaceholderStrategy, ISingletonDependency
    {
        public bool CanHandle(string placeholderKey)
        {
            return placeholderKey == "SEQ" || placeholderKey == "SEQ36";
        }

        public string Handle(string placeholderKey, SysSequenceRule rule, Dictionary<string, string> context)
        {
             if (placeholderKey == "SEQ") return rule.CurrentValue.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(rule.SeqLength, '0');
             if (placeholderKey == "SEQ36") return ConvertToBase36(rule.CurrentValue); 
             return placeholderKey;
        }

        private static string ConvertToBase36(long value)
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
