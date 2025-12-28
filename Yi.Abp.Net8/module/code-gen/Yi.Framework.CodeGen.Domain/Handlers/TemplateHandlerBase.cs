using Yi.Framework.CodeGen.Domain.Entities;

namespace Yi.Framework.CodeGen.Domain.Handlers
{
    public class TemplateHandlerBase
    {
        protected Table Table { get; set; }

        public void SetTable(Table table)
        {
            Table = table;
        }
    }
}
