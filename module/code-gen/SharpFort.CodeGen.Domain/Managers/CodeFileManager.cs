using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using Volo.Abp.Domain.Services;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Handlers;
using SharpFort.SqlSugarCore.Abstractions;
using TemplateContext = SharpFort.CodeGen.Domain.Handlers.TemplateContext;
using DbTemplate = SharpFort.CodeGen.Domain.Entities.Template;

namespace SharpFort.CodeGen.Domain.Managers
{
    /// <summary>
    /// 代码文件领域服务 (Web to Code)
    /// </summary>
    public class CodeFileManager : DomainService
    {
        private readonly IEnumerable<ITemplateContextEnricher> _enrichers;
        private readonly ISqlSugarRepository<DbTemplate> _templateRepository;
        private readonly ISqlSugarRepository<Table, Guid> _tableRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CodeFileManager> _logger;
        private readonly IncrementalCodeMerger _merger;

        public CodeFileManager(
            IEnumerable<ITemplateContextEnricher> enrichers,
            ISqlSugarRepository<DbTemplate> templateRepository,
            ISqlSugarRepository<Table, Guid> tableRepository,
            IConfiguration configuration,
            ILogger<CodeFileManager> logger)
        {
            _enrichers = enrichers;
            _templateRepository = templateRepository;
            _tableRepository = tableRepository;
            _configuration = configuration;
            _logger = logger;
            _merger = new IncrementalCodeMerger();
        }

        /// <summary>
        /// 根据模板名称推断目标子路径层
        /// </summary>
        private static string InferSubPath(string? templateName, string module)
        {
            string name = templateName ?? string.Empty;
            string ns = $"SharpFort.{module}";

            return name switch
            {
                "Entity" => $"{ns}.Domain/Entities",
                "GetListInput" or "GetListOutputDto" or "GetOutputDto" or "CreateInput" or "UpdateInput" =>
                    $"{ns}.Application.Contracts/Dtos",
                "IServices" => $"{ns}.Application.Contracts/IServices",
                "Service" => $"{ns}.Application/Services",
                _ => $"{ns}.Domain/Entities" // fallback
            };
        }

        public async Task BuildWebToCodeAsync(Table tableEntity)
        {
            // 1. 自动探测解决方案根目录 (SolutionRoot)
            string solutionRoot = SolutionDirectoryDetector.Detect(_configuration);
            _logger.LogInformation($"[CodeGen] 探测到解决方案根目录: {solutionRoot}");

            // 2. 加载全部模板
            List<DbTemplate> dbTemplates = await _templateRepository.GetListAsync();

            foreach (DbTemplate dbTemplate in dbTemplates)
            {
                try
                {
                    // 3. 混合模板加载逻辑：优先从本地工作区读取自定义文件模板
                    string localTemplateFolder = Path.Combine(solutionRoot, "module", "code-gen", "Templates");
                    string localTemplatePath = Path.Combine(localTemplateFolder, $"{dbTemplate.Name}.scriban");
                    if (!File.Exists(localTemplatePath))
                    {
                        // 尝试不带扩展名的读取
                        localTemplatePath = Path.Combine(localTemplateFolder, dbTemplate.Name!);
                    }

                    string templateContent = dbTemplate.Content!;

                    if (File.Exists(localTemplatePath))
                    {
                        templateContent = await File.ReadAllTextAsync(localTemplatePath);
                        _logger.LogInformation($"[CodeGen] 加载本地工作区覆写模板: {localTemplatePath}");
                    }

                    string renderedContent = string.Empty;
                    string relativeBuildPath = dbTemplate.BuildPath!;

                    // Scriban 引擎处理逻辑
                    TemplateContext contextModel = new();
                    foreach (var enricher in _enrichers.OrderBy(x => x.Priority))
                    {
                        enricher.Enrich(contextModel, tableEntity);
                    }

                    // 初始化 Scriban 变量脚本对象
                    var scriptObject = new ScriptObject();
                    scriptObject.Import(contextModel);
                    
                    // 注册全局自定义 C# 帮助函数
                    scriptObject.Import("sugar_column", new Func<FieldInfo, string>(ScribanHelperFunctions.SugarColumn));
                    scriptObject.Import("csharp_type", new Func<FieldInfo, string>(ScribanHelperFunctions.CsharpType));
                    scriptObject.Import("default_value", new Func<FieldInfo, string>(ScribanHelperFunctions.DefaultValue));

                    var renderContext = new Scriban.TemplateContext();
                    renderContext.PushGlobal(scriptObject);

                    // 渲染模板内容
                    var scribanTemplate = Scriban.Template.Parse(templateContent);
                    if (scribanTemplate.HasErrors)
                    {
                        throw new Exception($"[CodeGen] 模板 '{dbTemplate.Name}' 解析失败:\n" + string.Join("\n", scribanTemplate.Messages));
                    }
                    renderedContent = await scribanTemplate.RenderAsync(renderContext);

                    // 渲染生成相对路径 (BuildPath 支持占位符)
                    var pathTemplate = Scriban.Template.Parse(dbTemplate.BuildPath!);
                    relativeBuildPath = await pathTemplate.RenderAsync(renderContext);

                    // 自适应拼接绝对路径
                    relativeBuildPath = relativeBuildPath.Replace('\\', '/').TrimStart('/');
                    // 处理可能硬编码在旧模板里的绝对路径（如 D:/code/ 转换为相对路径）
                    if (Path.IsPathRooted(relativeBuildPath))
                    {
                        string fileName = Path.GetFileName(relativeBuildPath);
                        string module = tableEntity.ModuleName ?? "Rbac";
                        string? subPath = InferSubPath(dbTemplate.Name, module);
                        relativeBuildPath = $"module/{module}/{subPath}/{fileName}";
                        _logger.LogWarning($"[CodeGen] 旧模板 '{dbTemplate.Name}' 包含硬编码绝对路径，已推断为相对路径: {relativeBuildPath}");
                    }
                    
                    string absoluteBuildPath = Path.Combine(solutionRoot, relativeBuildPath);

                    // 增量安全合并写入
                    string? mergedContent = _merger.Merge(absoluteBuildPath, renderedContent);

                    if (mergedContent == null)
                    {
                        _logger.LogWarning($"[CodeGen] 已存在的文件无任何受保护区域标记，已跳过覆盖以防止代码丢失: {absoluteBuildPath}");
                        continue;
                    }

                    // 写入目标磁盘文件
                    string? dir = Path.GetDirectoryName(absoluteBuildPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    await File.WriteAllTextAsync(absoluteBuildPath, mergedContent);
                    _logger.LogInformation($"[CodeGen] 成功生成文件: {absoluteBuildPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[CodeGen] 渲染并生成模板 '{dbTemplate.Name}' 时发生错误！");
                    throw;
                }
            }

            // 记录最后代码生成时间
            tableEntity.LastBuildTime = DateTime.UtcNow;
            await _tableRepository.UpdateAsync(tableEntity);
        }
    }
}
