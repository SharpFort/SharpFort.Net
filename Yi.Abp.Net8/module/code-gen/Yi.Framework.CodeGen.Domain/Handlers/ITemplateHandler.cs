using Volo.Abp.DependencyInjection;
using Yi.Framework.CodeGen.Domain.Entities;

namespace Yi.Framework.CodeGen.Domain.Handlers
{
    public interface ITemplateHandler : ISingletonDependency
    {
        void SetTable(Table table);
        HandledTemplate Invoker(string str, string path);
    }
}
