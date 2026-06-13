using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.SqlSugarCore
{
    /// <summary>
    /// CodeGen 默认模板数据种子
    /// </summary>
    public class TemplateDataSeed : IDataSeedContributor, ITransientDependency
    {
        private readonly ISqlSugarRepository<Template> _repository;

        public TemplateDataSeed(ISqlSugarRepository<Template> repository)
        {
            _repository = repository;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            if (!await _repository.IsAnyAsync(x => true))
            {
                await _repository.InsertManyAsync(GetSeedData());
            }
        }

        public List<Template> GetSeedData()
        {
            var entities = new List<Template>();

            // 1. Dto GetListInput
            entities.Add(new Template(
                Guid.Parse("7fa2b98e-4a6c-48b4-8254-1b3260c6d7a1"),
                "GetListInput",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}GetListInput.cs",
                @"using SharpFort.Ddd.Application.Contracts;

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}}
{
    /// <summary>
    /// {{Table.Description}} 列表查询输入 DTO
    /// </summary>
    public class {{Model}}GetListInput : PagedAllResultRequestDto
    {
        /// <summary>
        /// 模糊关键字查询
        /// </summary>
        public string? Filter { get; set; }
    }
}"
            ));

            // 2. Dto GetListOutputDto
            entities.Add(new Template(
                Guid.Parse("8a4de9c2-cf47-4952-944f-c0c660f545a1"),
                "GetListOutputDto",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}GetListOutputDto.cs",
                @"using System;
using Volo.Abp.Application.Dtos;

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}}
{
    /// <summary>
    /// {{Table.Description}} 列表项返回 DTO
    /// </summary>
    public class {{Model}}GetListOutputDto : EntityDto<Guid>
    {
        {{~ for field in Fields ~}}
        {{~ if field.Name != ""Id"" ~}}
        /// <summary>
        /// {{field.Description}}
        /// </summary>
        public {{ field.CsharpType }} {{ field.Name }} { get; set; }
        {{~ end ~}}
        {{~ end ~}}
    }
}"
            ));

            // 3. Dto GetOutputDto
            entities.Add(new Template(
                Guid.Parse("96a5b9e0-cb41-477a-a232-c6f932e656d2"),
                "GetOutputDto",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}GetOutputDto.cs",
                @"using System;
using Volo.Abp.Application.Dtos;

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}}
{
    /// <summary>
    /// {{Table.Description}} 详情返回 DTO
    /// </summary>
    public class {{Model}}GetOutputDto : EntityDto<Guid>
    {
        {{~ for field in Fields ~}}
        {{~ if field.Name != ""Id"" ~}}
        /// <summary>
        /// {{field.Description}}
        /// </summary>
        public {{ field.CsharpType }} {{ field.Name }} { get; set; }
        {{~ end ~}}
        {{~ end ~}}
    }
}"
            ));

            // 4. Dto CreateInput
            entities.Add(new Template(
                Guid.Parse("a5e9b98e-4a6c-48b4-8254-1b3260c6d7a2"),
                "CreateInput",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}CreateInput.cs",
                @"using System;

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}}
{
    /// <summary>
    /// {{Table.Description}} 新增输入 DTO
    /// </summary>
    public class {{Model}}CreateInput
    {
        {{~ for field in Fields ~}}
        {{~ if field.Name != ""Id"" && field.Name != ""CreationTime"" ~}}
        /// <summary>
        /// {{field.Description}}
        /// </summary>
        public {{ field.CsharpType }} {{ field.Name }} { get; set; }
        {{~ end ~}}
        {{~ end ~}}
    }
}"
            ));

            // 5. Dto UpdateInput
            entities.Add(new Template(
                Guid.Parse("b6fa9c8e-4a6c-48b4-8254-1b3260c6d7a3"),
                "UpdateInput",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/Dtos/{{Model}}/{{Model}}UpdateInput.cs",
                @"using System;

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}}
{
    /// <summary>
    /// {{Table.Description}} 编辑修改输入 DTO
    /// </summary>
    public class {{Model}}UpdateInput
    {
        {{~ for field in Fields ~}}
        {{~ if field.Name != ""Id"" && field.Name != ""CreationTime"" ~}}
        /// <summary>
        /// {{field.Description}}
        /// </summary>
        public {{ field.CsharpType }} {{ field.Name }} { get; set; }
        {{~ end ~}}
        {{~ end ~}}
    }
}"
            ));

            // 6. IApplicationService 抽象
            entities.Add(new Template(
                Guid.Parse("c7fa2b9e-4a6c-48b4-8254-1b3260c6d7a4"),
                "IServices",
                "module/{{Module}}/SharpFort.{{Module}}.Application.Contracts/IServices/I{{Model}}Service.cs",
                @"using System;
using SharpFort.Ddd.Application.Contracts;
using {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}};

namespace {{RootNamespace}}.{{Module}}.Application.Contracts.IServices
{
    /// <summary>
    /// {{Table.Description}} 应用服务接口
    /// </summary>
    public interface I{{Model}}Service : ISfCrudAppService<{{Model}}GetOutputDto, {{Model}}GetListOutputDto, Guid, {{Model}}GetListInput, {{Model}}CreateInput, {{Model}}UpdateInput>
    {
    }
}"
            ));

            // 7. ApplicationService 实现
            entities.Add(new Template(
                Guid.Parse("d8fa2b9e-4a6c-48b4-8254-1b3260c6d7a5"),
                "Service",
                "module/{{Module}}/SharpFort.{{Module}}.Application/Services/{{Model}}Service.cs",
                @"using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;
using {{RootNamespace}}.{{Module}}.Domain.Entities;
using {{RootNamespace}}.{{Module}}.Application.Contracts.Dtos.{{Model}};
using {{RootNamespace}}.{{Module}}.Application.Contracts.IServices;

namespace {{RootNamespace}}.{{Module}}.Application.Services
{
    /// <summary>
    /// {{Table.Description}} 应用服务实现
    /// </summary>
    public partial class {{Model}}Service : SfCrudAppService<{{Model}}Entity, {{Model}}GetOutputDto, {{Model}}GetListOutputDto, Guid, {{Model}}GetListInput, {{Model}}CreateInput, {{Model}}UpdateInput>, I{{Model}}Service
    {
        private readonly ISqlSugarRepository<{{Model}}Entity, Guid> _repository;

        public {{Model}}Service(ISqlSugarRepository<{{Model}}Entity, Guid> repository) : base(repository)
        {
            _repository = repository;
        }

        // <sf-custom-code-start id=""CustomLogic"">
        // 在此区域中添加手写业务逻辑，重新生成代码时不会丢失
        // </sf-custom-code-start>
    }
}"
            ));

            return entities;
        }
    }
}
