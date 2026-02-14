using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using FluidSequence.Domain.Entities;

namespace FluidSequence.Domain.Services.Strategies
{
    public class ContextStrategy : IPlaceholderStrategy, ISingletonDependency
    {
        public bool CanHandle(string key)
        {
            return key == "UserCode" || key == "DeptCode" || key == "TenantCode" || key.StartsWith("Param:");
        }

        public string Handle(string key, SysSequenceRule rule, Dictionary<string, string> context)
        {
            if (context == null) return "";
            
            if (key.StartsWith("Param:"))
            {
                 var paramKey = key.Substring(6);
                 return context.ContainsKey(paramKey) ? context[paramKey] : "";
            }
            
            return context.ContainsKey(key) ? context[key] : "";
        }
    }
}
