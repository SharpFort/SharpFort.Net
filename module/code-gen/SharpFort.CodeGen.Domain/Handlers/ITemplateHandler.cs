using Volo.Abp.DependencyInjection;
using SharpFort.CodeGen.Domain.Entities;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public interface ITemplateHandler : ISingletonDependency
    {
        void SetTable(Table table);
        HandledTemplate Invoker(string str, string path);
    }
}
