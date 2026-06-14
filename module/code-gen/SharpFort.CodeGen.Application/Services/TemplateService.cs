using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using SharpFort.CodeGen.Application.Contracts.Dtos.Template;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Managers;
using SharpFort.Ddd.Application;
using SharpFort.SqlSugarCore.Abstractions;
using DbTemplate = SharpFort.CodeGen.Domain.Entities.Template;

namespace SharpFort.CodeGen.Application.Services;

/// <summary>
/// Scriban 代码生成模板 CRUD 服务
/// 管理代码生成模板库，支持查看、新增、编辑、删除 Scriban 模板内容及生成路径
/// 架构：DB 是运行时唯一数据源，本地 Templates/*.scriban 文件仅用于 Git 版本控制
/// 自动回写：Create/Update 操作保存 DB 后，自动同步写回本地 .scriban 文件
/// </summary>
public class TemplateService(
    ISqlSugarRepository<Template, Guid> repository,
    ISqlSugarRepository<DbTemplate> templateRepository,
    IConfiguration configuration,
    ILogger<TemplateService> logger
) : SfCrudAppService<Template, TemplateDto, Guid, TemplateGetListInput>(repository), ITemplateService
{
    private readonly ISqlSugarRepository<Template, Guid> _repository = repository;
    private readonly ISqlSugarRepository<DbTemplate> _templateRepository = templateRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TemplateService> _logger = logger;

    /// <summary>
    /// 分页查询模板列表：列出所有 Scriban 代码生成模板，支持按名称模糊筛选
    /// 默认模板 (7 个): GetListInput / GetListOutputDto / GetOutputDto / CreateInput / UpdateInput / IServices / Service
    /// </summary>
    /// <param name="input">查询参数：Name (可选，模板名称模糊筛选)</param>
    public override async Task<PagedResultDto<TemplateDto>> GetListAsync([FromQuery] TemplateGetListInput input)
    {
        RefAsync<int> total = 0;
        List<Template> entities = await _repository._DbQueryable.WhereIF(input.Name is not null, x => x.Name!.Contains(input.Name!))
                  .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        return new PagedResultDto<TemplateDto>
        {
            TotalCount = total,
            Items = await MapToGetListOutputDtosAsync(entities)
        };
    }

    /// <summary>
    /// 获取模板详情：查看单个 Scriban 模板的完整内容（Scriban 脚本）和生成路径规则 (BuildPath)
    /// </summary>
    /// <param name="id">模板 ID</param>
    public override async Task<TemplateDto> GetAsync(Guid id)
    {
        await CheckGetPolicyAsync();

        Template entity = await _repository._DbQueryable
            .Where(x => x.Id == id)
            .FirstAsync() ?? throw new EntityNotFoundException(typeof(Template), id);

        return await MapToGetOutputDtoAsync(entity);
    }

    /// <summary>
    /// 创建新模板：添加自定义 Scriban 代码生成模板
    /// 场景：新增前端 API 模板 (xxxApi.js)、额外的 DTO 模板、自定义 Entity 模板等
    /// 自动回写：保存 DB 后同步写入本地 Templates/{Name}.scriban 文件（Git 版本控制）
    /// </summary>
    /// <param name="input">创建输入 DTO（模板名称 + Scriban 内容 + 生成路径）</param>
    public override async Task<TemplateDto> CreateAsync(TemplateDto input)
    {
        await CheckCreatePolicyAsync();
        await CheckCreateInputDtoAsync(input);

        Template entity = await MapToEntityAsync(input);
        await Repository.InsertAsync(entity, autoSave: true);

        // 自动回写本地 .scriban 文件
        WriteBackLocalTemplate(entity);

        return await MapToGetOutputDtoAsync(entity);
    }

    /// <summary>
    /// 更新模板：修改 Scriban 模板内容或生成路径 (BuildPath)
    /// 自动回写：保存 DB 后同步写入本地 Templates/{Name}.scriban 文件（Git 版本控制）
    /// </summary>
    /// <param name="id">模板 ID</param>
    /// <param name="input">更新输入 DTO</param>
    public override async Task<TemplateDto> UpdateAsync(Guid id, TemplateDto input)
    {
        await CheckUpdatePolicyAsync();

        Template entity = await GetEntityByIdAsync(id);
        await CheckUpdateInputDtoAsync(entity, input);
        await MapToEntityAsync(input, entity);
        await Repository.UpdateAsync(entity, autoSave: true);

        // 自动回写本地 .scriban 文件
        WriteBackLocalTemplate(entity);

        return await MapToGetOutputDtoAsync(entity);
    }

    #region 禁用端点 — 模板管理不需要下拉框/Excel 导入导出

    /// <summary>
    /// [已禁用] 无其他表单引用 Template 做下拉选择，代码生成时模板选择由系统内部逻辑控制
    /// </summary>
    [RemoteService(isEnabled: false)]
    public override Task<PagedResultDto<TemplateDto>> GetSelectDataListAsync(string? keywords = null) => throw new NotImplementedException();

    /// <summary>
    /// [已禁用] Scriban 模板内容是代码脚本，用 Excel 展示/编辑体验极差，应使用本地 .scriban 文件管理
    /// </summary>
    [RemoteService(isEnabled: false)]
    public override Task<IActionResult> GetExportExcelAsync(TemplateGetListInput input) => throw new NotImplementedException();

    /// <summary>
    /// [已禁用] Scriban 代码不适合通过 Excel 导入，且基类未实现
    /// </summary>
    [RemoteService(isEnabled: false)]
    public override Task PostImportExcelAsync(List<TemplateDto> input) => throw new NotImplementedException();

    #endregion

    #region 模板同步端点 — import/export

    /// <summary>
    /// 导入模板（本地 → DB）：扫描本地 Templates/*.scriban 文件，增量同步到 gen_template 表
    /// 规则（Upsert by Name）：
    ///   本地存在 + DB 不存在 → INSERT 新模板
    ///   本地存在 + DB 已存在 → UPDATE Content（保留用户 Remarks）
    ///   本地不存在 + DB 存在 → 跳过（不删除，由用户手动管理）
    /// </summary>
    public async Task PostImportTemplatesAsync()
    {
        string solutionRoot = SolutionDirectoryDetector.Detect(_configuration);
        string templateFolder = Path.Combine(solutionRoot, "module", "code-gen", "Templates");

        if (!Directory.Exists(templateFolder))
        {
            throw new UserFriendlyException($"[CodeGen] 模板目录不存在: {templateFolder}");
        }

        string[] scribanFiles = Directory.GetFiles(templateFolder, "*.scriban");
        if (scribanFiles.Length == 0)
        {
            throw new UserFriendlyException($"[CodeGen] 模板目录为空: {templateFolder}");
        }

        List<DbTemplate> dbTemplates = await _templateRepository.GetListAsync();
        Dictionary<string, DbTemplate> dbMap = dbTemplates.ToDictionary(t => t.Name!, StringComparer.OrdinalIgnoreCase);

        int inserted = 0, updated = 0;

        foreach (string filePath in scribanFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string content = await File.ReadAllTextAsync(filePath);

            if (dbMap.TryGetValue(fileName, out DbTemplate? existing))
            {
                // UPDATE: 仅更新 Content，保留用户 Remarks
                existing.SetContent(content);
                await _templateRepository.UpdateAsync(existing);
                updated++;
                _logger.LogInformation($"[CodeGen] import 更新模板: {fileName}");
            }
            else
            {
                // INSERT: 新模板入库
                string buildPath = $"module/{{{{project_name}}}}/SharpFort.{{{{project_name}}}}.Application/Services/{{{{Model}}}}{fileName}.cs";
                DbTemplate newTemplate = new(Guid.NewGuid(), fileName, buildPath, content);
                await _templateRepository.InsertAsync(newTemplate);
                inserted++;
                _logger.LogInformation($"[CodeGen] import 新增模板: {fileName}");
            }
        }

        _logger.LogInformation($"[CodeGen] import 完成: 新增 {inserted} 个, 更新 {updated} 个, 总计 {scribanFiles.Length} 个模板");
    }

    /// <summary>
    /// 导出模板（DB → 本地）：遍历数据库中所有模板，写入本地 Templates/{Name}.scriban 文件
    /// 规则：
    ///   DB 存在 + 本地不存在 → 写入本地文件（补全缺失）
    ///   DB 存在 + 本地存在   → 覆写本地文件（保持一致）
    /// </summary>
    public async Task PostExportTemplatesAsync()
    {
        string solutionRoot = SolutionDirectoryDetector.Detect(_configuration);
        string templateFolder = Path.Combine(solutionRoot, "module", "code-gen", "Templates");

        if (!Directory.Exists(templateFolder))
        {
            Directory.CreateDirectory(templateFolder);
            _logger.LogInformation($"[CodeGen] 创建模板目录: {templateFolder}");
        }

        List<DbTemplate> dbTemplates = await _templateRepository.GetListAsync();
        if (dbTemplates.Count == 0)
        {
            throw new UserFriendlyException("[CodeGen] 数据库中没有任何模板数据，请先执行 import-templates 导入模板");
        }

        int exported = 0;

        foreach (DbTemplate template in dbTemplates)
        {
            string filePath = Path.Combine(templateFolder, $"{template.Name}.scriban");
            await File.WriteAllTextAsync(filePath, template.Content!);
            exported++;
            _logger.LogInformation($"[CodeGen] export 写入: {template.Name}.scriban");
        }

        _logger.LogInformation($"[CodeGen] export 完成: 导出 {exported} 个模板到 {templateFolder}");
    }

    #endregion

    #region 私有方法 — 本地文件回写

    /// <summary>
    /// 将模板内容回写到本地 Templates/{Name}.scriban 文件（Git 版本控制）
    /// 回写失败仅记录 Warning 日志，不阻断 DB 保存
    /// </summary>
    private void WriteBackLocalTemplate(Template entity)
    {
        try
        {
            string solutionRoot = SolutionDirectoryDetector.Detect(_configuration);
            string templateFolder = Path.Combine(solutionRoot, "module", "code-gen", "Templates");

            if (!Directory.Exists(templateFolder))
            {
                Directory.CreateDirectory(templateFolder);
            }

            string filePath = Path.Combine(templateFolder, $"{entity.Name}.scriban");
            File.WriteAllText(filePath, entity.Content!);
            _logger.LogInformation($"[CodeGen] 回写本地模板: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[CodeGen] 回写本地模板失败: {entity.Name}，DB 数据已保存");
        }
    }

    #endregion
}
