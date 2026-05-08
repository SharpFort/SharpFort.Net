using Microsoft.Extensions.CommandLineUtils;
using Volo.Abp.DependencyInjection;

namespace SharpFort.Tool
{
    public interface ICommand : ISingletonDependency
    {
        public string Command { get; }

        public string? Description { get; }
        void CommandLineApplication(CommandLineApplication application);

    }
}
