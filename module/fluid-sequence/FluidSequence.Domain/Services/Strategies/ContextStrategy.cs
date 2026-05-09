using Volo.Abp.DependencyInjection;
using SharpFort.FluidSequence.Domain.Entities;

namespace SharpFort.FluidSequence.Domain.Services.Strategies
{
    public class ContextStrategy : IPlaceholderStrategy, ISingletonDependency
    {
        public bool CanHandle(string placeholderKey)
        {
            return placeholderKey == "UserCode" || placeholderKey == "DeptCode" || placeholderKey == "TenantCode" || placeholderKey.StartsWith("Param:", System.StringComparison.Ordinal);
        }

        public string Handle(string placeholderKey, SysSequenceRule rule, Dictionary<string, string> context)
        {
            if (context == null)
            {
                return "";
            }

            if (placeholderKey.StartsWith("Param:", System.StringComparison.Ordinal))
            {
                var paramKey = placeholderKey[6..];
                return context.TryGetValue(paramKey, out var val1) ? val1 : "";
            }

            return context.TryGetValue(placeholderKey, out var val2) ? val2 : "";
        }
    }
}
