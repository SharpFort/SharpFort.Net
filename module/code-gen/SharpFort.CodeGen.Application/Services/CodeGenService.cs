using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Managers;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// CodeGen 代码生成核心服务
    /// 提供实体注册表与 Scriban 模板之间的代码生成能力
    /// </summary>
    public class CodeGenService : ApplicationService, ICodeGenService
    {
        private readonly ISqlSugarRepository<Table, Guid> _tableRepository;
        private readonly CodeFileManager _codeFileManager;
        private readonly WebTemplateManager _webTemplateManager;
        private readonly ILogger<CodeGenService> _logger;

        public CodeGenService(
            ISqlSugarRepository<Table, Guid> tableRepository,
            CodeFileManager codeFileManager,
            WebTemplateManager webTemplateManager,
            ILogger<CodeGenService> logger)
        {
            _tableRepository = tableRepository;
            _codeFileManager = codeFileManager;
            _webTemplateManager = webTemplateManager;
            _logger = logger;
        }

        /// <summary>
        /// 生成代码 (Web → Code)：根据选中的实体注册表条目，使用 Scriban 模板生成 DTO / IService / Service 代码文件
        /// </summary>
        /// <param name="ids">选中的实体注册表 ID 列表</param>
        public async Task PostWebBuildCodeAsync(List<Guid> ids)
        {
            List<Table> tables = await _tableRepository._DbQueryable
                .Where(x => ids.Contains(x.Id))
                .Includes(x => x.Fields)
                .ToListAsync();

            foreach (Table table in tables)
            {
                await _codeFileManager.BuildWebToCodeAsync(table);
            }
        }


        /// <summary>
        /// 同步实体到注册表 (Code → Web)：反射扫描所有带 [SugarTable] 特性的 C# 实体类，增量合并到 YiTable 注册表
        /// </summary>
        [UnitOfWork]
        public async Task PostCodeBuildWebAsync()
        {
            await _webTemplateManager.BuildCodeToWebAsync();
        }

        /// <summary>
        /// 手动刷新实体注册表：重新扫描所有实体类并增量同步到 YiTable，用于实体代码变更后立即更新注册表
        /// </summary>
        [UnitOfWork]
        public async Task PostRefreshAsync()
        {
            _logger.LogInformation("[CodeGen] 手动刷新实体注册表...");
            await PostCodeBuildWebAsync();
            _logger.LogInformation("[CodeGen] 刷新完成！");
        }

        /// <summary>
        /// 打开本地目录：在系统文件管理器中打开指定路径 (支持 Windows/Linux/macOS)
        /// </summary>
        /// <param name="path">要打开的目录相对路径</param>
        [HttpPost("code-gen/dir/{**path}")]
#pragma warning disable CA1822
        public Task PostDir([FromRoute] string path)
        {
            path = Uri.UnescapeDataString(path);
            path = string.Join(Path.DirectorySeparatorChar, path.Split('/', '\\').Where(x => !x.Contains('@')));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", path);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", path);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", path);
            else
                throw new UserFriendlyException("当前操作系统不支持打开目录");

            return Task.CompletedTask;
        }
#pragma warning restore CA1822
    }
}
