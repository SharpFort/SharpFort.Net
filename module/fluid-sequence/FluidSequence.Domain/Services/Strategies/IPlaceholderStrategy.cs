using SharpFort.FluidSequence.Domain.Entities;

namespace SharpFort.FluidSequence.Domain.Services.Strategies
{
    public interface IPlaceholderStrategy
    {
        bool CanHandle(string placeholderKey);
        string Handle(string placeholderKey, SysSequenceRule rule, Dictionary<string, string> context);
    }
}
