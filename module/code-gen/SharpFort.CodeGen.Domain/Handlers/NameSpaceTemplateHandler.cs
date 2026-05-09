namespace SharpFort.CodeGen.Domain.Handlers
{
    public class NameSpaceTemplateHandler : TemplateHandlerBase, ITemplateHandler
    {
        public HandledTemplate Invoker(string str, string path)
        {
            HandledTemplate output = new HandledTemplate
            {
                TemplateStr = str.Replace("@namespace", ""),
                BuildPath = path
            };
            return output;
        }
    }
}
