using System.Collections.Generic;
using FluidSequence.Domain.Entities;

namespace FluidSequence.Domain.Services.Strategies
{
    public interface IPlaceholderStrategy
    {
        bool CanHandle(string placeholderKey);
        string Handle(string placeholderKey, SysSequenceRule rule, Dictionary<string, string> context);
    }
}
