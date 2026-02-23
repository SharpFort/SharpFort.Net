using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    public interface ITemplateService : ISfCrudAppService<TemplateDto, Guid, TemplateGetListInput>
    {
    }
}
