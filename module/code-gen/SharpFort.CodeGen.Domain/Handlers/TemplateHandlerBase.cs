using SharpFort.CodeGen.Domain.Entities;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class TemplateHandlerBase
    {
        protected Table Table { get; set; } = null!;

        public void SetTable(Table table)
        {
            Table = table;
        }
    }
}
