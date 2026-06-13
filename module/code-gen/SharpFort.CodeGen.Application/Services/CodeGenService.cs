using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;
using Volo.Abp.Guids;
using SharpFort.CodeGen.Application.Contracts.IServices;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Managers;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Application.Services
{
    /// <summary>
    /// CodeGen 核心应用服务
    /// </summary>
    public class CodeGenService : ApplicationService, ICodeGenService
    {
        private readonly ISqlSugarRepository<Table, Guid> _tableRepository;
        private readonly ISqlSugarRepository<Field, Guid> _fieldRepository;
        private readonly CodeFileManager _codeFileManager;
        private readonly WebTemplateManager _webTemplateManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<CodeGenService> _logger;

        public CodeGenService(
            ISqlSugarRepository<Table, Guid> tableRepository,
            ISqlSugarRepository<Field, Guid> fieldRepository,
            CodeFileManager codeFileManager,
            WebTemplateManager webTemplateManager,
            IGuidGenerator guidGenerator,
            ILogger<CodeGenService> logger)
        {
            _tableRepository = tableRepository;
            _fieldRepository = fieldRepository;
            _codeFileManager = codeFileManager;
            _webTemplateManager = webTemplateManager;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        /// <summary>
        /// Web To Code (渲染生成文件)
        /// </summary>
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
        /// Code To Web (反射 C# 扫描并增量同步到实体注册表)
        /// </summary>
        [UnitOfWork]
        public async Task PostCodeBuildWebAsync()
        {
            await _webTemplateManager.BuildCodeToWebAsync();
        }

        /// <summary>
        /// 手动刷新实体注册表 (重新扫描所有实体类并增量同步)
        /// </summary>
        [UnitOfWork]
        public async Task PostRefreshAsync()
        {
            _logger.LogInformation("[CodeGen] 手动刷新实体注册表...");
            await PostCodeBuildWebAsync();
            _logger.LogInformation("[CodeGen] 刷新完成！");
        }

        /// <summary>
        /// DB To Web (DB-First 逆向物理表结构生成 Web 配置元数据)
        /// </summary>
        [UnitOfWork]
        public async Task PostDbToWebAsync(string tableName, string? moduleName = null, string? rootNamespace = null)
        {
            tableName = Volo.Abp.Check.NotNullOrWhiteSpace(tableName, nameof(tableName));
            bool tableExists = _tableRepository._Db.DbMaintenance.IsAnyTable(tableName, false);
            if (!tableExists)
            {
                throw new UserFriendlyException($"[CodeGen] 数据库中未找到物理表: {tableName}");
            }

            var tableInfos = _tableRepository._Db.DbMaintenance.GetTableInfoList();
            var matchedTable = tableInfos.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
            string tableDesc = matchedTable?.Description ?? string.Empty;

            // 获取物理字段定义
            var columns = _tableRepository._Db.DbMaintenance.GetColumnInfosByTableName(tableName, false);

            // 清理已存在的同名元数据，避免冲突
            var oldTable = await _tableRepository._DbQueryable.Where(x => x.Name == ToPascalCase(tableName)).FirstAsync();
            if (oldTable != null)
            {
                await _tableRepository.DeleteAsync(oldTable.Id);
                await _fieldRepository._Db.Deleteable<Field>().Where(f => f.TableId == oldTable.Id).ExecuteCommandAsync();
            }

            Guid tableId = _guidGenerator.Create();
            Table newTable = new(tableId, ToPascalCase(tableName), tableDesc)
            {
                ModuleName = moduleName ?? "Rbac",
                RootNamespace = rootNamespace ?? "Sf.Abp",
                IsOverwrite = true
            };

            int order = 1;
            List<Field> newFields = [];
            foreach (var col in columns)
            {
                Guid fieldId = _guidGenerator.Create();
                Field field = new(fieldId, tableId, ToPascalCase(col.DbColumnName), MapDbTypeToFieldType(col.DataType))
                {
                    Description = col.ColumnDescription ?? string.Empty,
                    Length = col.Length,
                    OrderNum = order++,
                    IsRequired = !col.IsNullable,
                    IsKey = col.IsPrimarykey,
                    IsAutoAdd = col.IsIdentity,
                    IsPublic = IsCommonField(col.DbColumnName),
                    IsQueryField = !col.IsPrimarykey && !IsCommonField(col.DbColumnName),
                    IsListDisplay = !col.IsPrimarykey,
                    IsFormItem = !col.IsPrimarykey && !col.IsIdentity && !IsCommonField(col.DbColumnName),
                    HtmlType = "Input"
                };
                newFields.Add(field);
            }

            newTable.Fields = newFields;
            await _tableRepository._Db.InsertNav(newTable).Include(x => x.Fields).ExecuteCommandAsync();
            _logger.LogInformation($"[CodeGen DB-First] 成功将物理表 '{tableName}' 逆向同步至 Web 模板元数据，元数据表 ID: {tableId}");
        }

        /// <summary>
        /// 打开本地目录 (仅支持 Windows 开发环境)
        /// </summary>
        [HttpPost("code-gen/dir/{**path}")]
#pragma warning disable CA1822
        public Task PostDir([FromRoute] string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = Uri.UnescapeDataString(path);
                path = string.Join('\\', path.Split('\\').Where(x => !x.Contains('@')));
                Process.Start("explorer.exe", path);
            }
            else
            {
                throw new UserFriendlyException("当前操作系统不支持打开目录");
            }

            return Task.CompletedTask;
        }
#pragma warning restore CA1822

        #region 私有辅助工具

        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // 剥除常见数据库前缀
            if (input.StartsWith("sys_", StringComparison.OrdinalIgnoreCase)) input = input[4..];
            if (input.StartsWith("gen_", StringComparison.OrdinalIgnoreCase)) input = input[4..];
            if (input.StartsWith("sf_", StringComparison.OrdinalIgnoreCase)) input = input[3..];

            string[] parts = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parts.Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));
        }

        private static FieldType MapDbTypeToFieldType(string sqlDataType)
        {
            sqlDataType = sqlDataType.ToLowerInvariant();
            if (sqlDataType.Contains("varchar") || sqlDataType.Contains("text") || sqlDataType.Contains("char") || sqlDataType.Contains("string"))
            {
                return FieldType.String;
            }
            if (sqlDataType.Contains("int8") || sqlDataType.Contains("bigint") || sqlDataType.Contains("long"))
            {
                return FieldType.Long;
            }
            if (sqlDataType.Contains("int") || sqlDataType.Contains("integer") || sqlDataType.Contains("smallint"))
            {
                return FieldType.Int;
            }
            if (sqlDataType.Contains("bool") || sqlDataType.Contains("bit") || sqlDataType.Contains("boolean"))
            {
                return FieldType.Bool;
            }
            if (sqlDataType.Contains("decimal") || sqlDataType.Contains("numeric") || sqlDataType.Contains("double") || sqlDataType.Contains("float"))
            {
                return FieldType.Decimal;
            }
            if (sqlDataType.Contains("date") || sqlDataType.Contains("time"))
            {
                return FieldType.DateTime;
            }
            if (sqlDataType.Contains("uuid") || sqlDataType.Contains("guid") || sqlDataType.Contains("uniqueidentifier"))
            {
                return FieldType.Guid;
            }
            return FieldType.String;
        }

        private static bool IsCommonField(string columnName)
        {
            columnName = columnName.ToLowerInvariant();
            string[] commonFields = ["id", "creationtime", "creatorid", "lastmodificationtime", "lastmodifierid", "isdeleted", "deleterid", "deletiontime", "tenantid"];
            return commonFields.Contains(columnName);
        }

        #endregion
    }
}
