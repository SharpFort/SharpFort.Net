using Volo.Abp.DependencyInjection;
using SharpFort.CodeGen.Domain.Entities;

namespace SharpFort.CodeGen.Domain.Handlers
{
    /// <summary>
    /// 模板上下文富化器，向 Scriban 渲染上下文贡献数据
    /// </summary>
    public interface ITemplateContextEnricher : ISingletonDependency
    {
        void Enrich(TemplateContext context, Table table);
        
        /// <summary>
        /// 优先级，数值小的优先执行
        /// </summary>
        int Priority { get; }
    }
}
