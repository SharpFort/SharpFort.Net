using Microsoft.Extensions.CommandLineUtils;
using Volo.Abp.DependencyInjection;

namespace SharpFort.Tool
{
    public interface ICommand : ISingletonDependency
    {
        string Command { get; }

        string? Description { get; }
        void CommandLineApplication(CommandLineApplication application);

    }
}
