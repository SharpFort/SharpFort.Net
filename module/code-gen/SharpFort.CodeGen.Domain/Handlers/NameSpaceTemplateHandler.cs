namespace SharpFort.CodeGen.Domain.Handlers
{
    public class NameSpaceTemplateHandler : TemplateHandlerBase, ITemplateHandler
    {
        public HandledTemplate Invoker(string str, string path)
        {
            var output = new HandledTemplate
            {
                TemplateStr = str.Replace("@namespace", ""),
                BuildPath = path
            };
            return output;
        }
    }
}
