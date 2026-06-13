using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SharpFort.CodeGen.Application.Contracts.IServices
{
    public interface ICodeGenService : IApplicationService
    {
        Task PostWebBuildCodeAsync(List<Guid> ids);

        Task PostCodeBuildWebAsync();

        Task PostRefreshAsync();

        Task PostDbToWebAsync(string tableName, string? moduleName = null, string? rootNamespace = null);
    }
}
