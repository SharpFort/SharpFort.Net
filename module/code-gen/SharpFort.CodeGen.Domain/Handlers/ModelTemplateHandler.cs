namespace SharpFort.CodeGen.Domain.Handlers
{
    public class ModelTemplateHandler : TemplateHandlerBase, ITemplateHandler
    {
        public HandledTemplate Invoker(string str, string path)
        {
            HandledTemplate output = new()
            {
                TemplateStr = str.Replace("@model", char.ToLowerInvariant(Table.Name[0]) + Table.Name[1..]).Replace("@Model", char.ToUpperInvariant(Table.Name[0]) + Table.Name[1..]),
                BuildPath = path.Replace("@model", char.ToLowerInvariant(Table.Name[0]) + Table.Name[1..]).Replace("@Model", char.ToUpperInvariant(Table.Name[0]) + Table.Name[1..])
            };
            return output;
        }
    }
}
