using Volo.Abp.Domain.Services;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Handlers;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Domain.Managers
{
    /// <summary>
    /// 代码文件领域服务,与代码文件生成相关，web to code
    /// </summary>
    public class CodeFileManager : DomainService
    {
        private IEnumerable<ITemplateHandler> _templateHandlers;
        private ISqlSugarRepository<Template> _repository;
        private ISqlSugarRepository<Field> _fieldRepository;
        public CodeFileManager(IEnumerable<ITemplateHandler> templateHandlers, ISqlSugarRepository<Field> fieldRepository, ISqlSugarRepository<Template> repository)
        {
            _templateHandlers = templateHandlers;
            _repository = repository;
            _fieldRepository = fieldRepository;
        }
        public async Task BuildWebToCodeAsync(Table tableEntity)
        {
            var templates = await _repository.GetListAsync();
            foreach (var template in templates)
            {
                var handledTempalte = new HandledTemplate();
                handledTempalte.TemplateStr = template.Content;
                handledTempalte.BuildPath = template.BuildPath;
                foreach (var templateHandler in _templateHandlers)
                {
                    templateHandler.SetTable(tableEntity);
                    handledTempalte = templateHandler.Invoker(handledTempalte.TemplateStr, handledTempalte.BuildPath);
                }
                await BuildToFileAsync(handledTempalte);

            }
        }


        private async Task BuildToFileAsync(HandledTemplate handledTemplate)
        {
            if (!Directory.Exists(Path.GetDirectoryName(handledTemplate.BuildPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(handledTemplate.BuildPath));
            }
            await File.WriteAllTextAsync(handledTemplate.BuildPath, handledTemplate.TemplateStr);
        }


    }

}
