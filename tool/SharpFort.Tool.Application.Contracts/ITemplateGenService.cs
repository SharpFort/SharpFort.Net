using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using SharpFort.Tool.Application.Contracts.Dtos;

namespace SharpFort.Tool.Application.Contracts
{
    public interface ITemplateGenService: IApplicationService
    {
        Task<byte[]> CreateModuleAsync(TemplateGenCreateInputDto moduleCreateInputDto);
        Task<List<string>> GetAllTemplatesAsync();
    }
}
