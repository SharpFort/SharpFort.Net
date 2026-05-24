using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;
using Volo.Abp.Data;
using Volo.Abp.Guids;
using Volo.Abp.Users;
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
        private readonly IModuleContainer _moduleContainer;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<CodeGenService> _logger;

        public CodeGenService(
            ISqlSugarRepository<Table, Guid> tableRepository,
            ISqlSugarRepository<Field, Guid> fieldRepository,
            CodeFileManager codeFileManager,
            WebTemplateManager webTemplateManager,
            IModuleContainer moduleContainer,
            IGuidGenerator guidGenerator,
            ICurrentUser currentUser,
            ILogger<CodeGenService> logger)
        {
            _tableRepository = tableRepository;
            _fieldRepository = fieldRepository;
            _codeFileManager = codeFileManager;
            _webTemplateManager = webTemplateManager;
            _moduleContainer = moduleContainer;
            _guidGenerator = guidGenerator;
            _currentUser = currentUser;
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
        /// Web To Db (Scaffolding 在线物理表设计更新，安全 DDL 护栏)
        /// </summary>
        [Authorize(Roles = "admin")]
        [UnitOfWork]
        public async Task<string> PostWebBuildDbAsync(List<Guid> ids, bool dryRun = false)
        {
            List<Table> tables = await _tableRepository._DbQueryable
                .Where(x => ids.Contains(x.Id))
                .Includes(x => x.Fields)
                .ToListAsync();

            StringBuilder ddlBuilder = new();

            foreach (Table table in tables)
            {
                if (string.IsNullOrWhiteSpace(table.Name)) continue;

                bool tableExists = _tableRepository._Db.DbMaintenance.IsAnyTable(table.Name, false);
                if (!tableExists)
                {
                    // CREATE TABLE
                    var colDefinitions = table.Fields.OrderBy(x => x.OrderNum).Select(GetColumnSqlDefinition);
                    string createSql = $"CREATE TABLE \"{table.Name}\" (\n  {string.Join(",\n  ", colDefinitions)}\n);\n";
                    ddlBuilder.AppendLine(createSql);
                }
                else
                {
                    // ALTER TABLE (ADD COLUMN / ALTER COLUMN)
                    var physicalColumns = _tableRepository._Db.DbMaintenance.GetColumnInfosByTableName(table.Name, false);
                    
                    foreach (var genField in table.Fields.OrderBy(x => x.OrderNum))
                    {
                        if (string.IsNullOrWhiteSpace(genField.Name)) continue;

                        var physicalCol = physicalColumns.FirstOrDefault(c => string.Equals(c.DbColumnName, genField.Name, StringComparison.OrdinalIgnoreCase));
                        if (physicalCol == null)
                        {
                            // ADD COLUMN
                            string def = GetColumnSqlDefinition(genField);
                            string addSql = $"ALTER TABLE \"{table.Name}\" ADD COLUMN {def};\n";
                            ddlBuilder.AppendLine(addSql);
                        }
                        else
                        {
                            // Compare and ALTER COLUMN if changed
                            var connDbType = _tableRepository._Db.CurrentConnectionConfig.DbType;
                            string dbTypeName = GetDbTypeName(genField.FieldType, connDbType);
                            bool isNullableChanged = physicalCol.IsNullable != !genField.IsRequired;
                            bool isTypeChanged = !physicalCol.DataType.Contains(dbTypeName, StringComparison.OrdinalIgnoreCase);

                            if (isNullableChanged || isTypeChanged)
                            {
                                string alterSql = GetAlterColumnSql(table.Name, genField);
                                ddlBuilder.AppendLine(alterSql);
                            }
                        }
                    }
                }
            }

            string totalSql = ddlBuilder.ToString();

            // 审计日志记录真实用户身份
            _logger.LogInformation($"[CodeGen DDL Audit] 用户 {_currentUser.UserName ?? "未知"} 触发表结构同步。DryRun: {dryRun}，SQL 内容:\n{totalSql}");

            if (dryRun)
            {
                return totalSql;
            }

            // 显式 DDL DROP 安全拦截
            if (totalSql.Contains("DROP", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("禁止的DDL操作：代码生成不允许执行DROP语句。如需删除表/列，请手动操作数据库。");
            }

            if (!string.IsNullOrWhiteSpace(totalSql))
            {
                // 逐条拆分执行，兼容 PostgreSQL Npgsql 等不支持多语句的驱动
                string[] statements = totalSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await _tableRepository._Db.Ado.UseTranAsync(async () =>
                {
                    foreach (string stmt in statements)
                    {
                        if (!string.IsNullOrWhiteSpace(stmt))
                        {
                            await _tableRepository._Db.Ado.ExecuteCommandAsync(stmt.TrimEnd() + ";");
                        }
                    }
                });
            }

            return "同步成功！";
        }

        /// <summary>
        /// Code To Web (反射 C# 扫描并同步到 Scaffolder 元数据配置中)
        /// </summary>
        [UnitOfWork]
        public async Task PostCodeBuildWebAsync()
        {
            List<Table> tables = await _webTemplateManager.BuildCodeToWebAsync();
            
            // 覆盖式更新
            _tableRepository._Db.DbMaintenance.TruncateTable<Table>();
            _tableRepository._Db.DbMaintenance.TruncateTable<Field>();

            foreach (var table in tables)
            {
                table.Fields.ForEach(x =>
                {
                    x.IsQueryField = true;
                    x.IsListDisplay = true;
                    x.IsFormItem = true;
                    x.HtmlType = "Input";
                });
            }

            await _tableRepository._Db.InsertNav(tables).Include(x => x.Fields).ExecuteCommandAsync();
        }

        /// <summary>
        /// Code To Db (扫描 C# 实体类通过 CodeFirst 同步物理表)
        /// </summary>
        public async Task PostCodeBuildDbAsync()
        {
            List<Type> entityTypes = [];
            foreach (IAbpModuleDescriptor module in _moduleContainer.Modules)
            {
                entityTypes.AddRange(module.Assembly.GetTypes()
                    .Where(x => x.GetCustomAttribute<IgnoreCodeFirstAttribute>() == null)
                    .Where(x => x.GetCustomAttribute<SugarTable>() != null)
                    .Where(x => x.GetCustomAttribute<SplitTableAttribute>() is null));
            }

            if (entityTypes.Count > 0)
            {
                _logger.LogInformation($"[CodeGen Code-First] 正在同步 {entityTypes.Count} 个实体到物理数据库...");
                _tableRepository._Db.CodeFirst.InitTables(entityTypes.ToArray());
            }
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

        private string GetColumnSqlDefinition(Field field)
        {
            var dbType = _tableRepository._Db.CurrentConnectionConfig.DbType;
            string typeName = MapFieldTypeToSqlType(field.FieldType, field.Length, dbType);

            string pk = field.IsKey ? " PRIMARY KEY" : "";
            string nullable = (!field.IsRequired && !field.IsKey) ? " NULL" : " NOT NULL";

            return $"\"{field.Name}\" {typeName}{pk}{nullable}";
        }

        private string GetAlterColumnSql(string tableName, Field field)
        {
            var dbType = _tableRepository._Db.CurrentConnectionConfig.DbType;
            string typeName = MapFieldTypeToSqlType(field.FieldType, field.Length, dbType);

            string nullable = (!field.IsRequired && !field.IsKey) ? "NULL" : "NOT NULL";

            return dbType switch
            {
                DbType.PostgreSQL => $"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{field.Name}\" TYPE {typeName}, ALTER COLUMN \"{field.Name}\" {(field.IsRequired ? "SET NOT NULL" : "DROP NOT NULL")};\n",
                DbType.MySql => $"ALTER TABLE `{tableName}` MODIFY COLUMN `{field.Name}` {typeName} {nullable};\n",
                _ => $"ALTER TABLE [{tableName}] ALTER COLUMN [{field.Name}] {typeName} {nullable};\n"
            };
        }

        private static string MapFieldTypeToSqlType(FieldType type, int length, DbType dbType)
        {
            return dbType switch
            {
                DbType.PostgreSQL => type switch
                {
                    FieldType.String => length > 0 ? $"VARCHAR({length})" : "TEXT",
                    FieldType.Int => "INT",
                    FieldType.Long => "BIGINT",
                    FieldType.Bool => "BOOLEAN",
                    FieldType.Decimal => "DECIMAL(18,2)",
                    FieldType.DateTime => "TIMESTAMP",
                    FieldType.Guid => "UUID",
                    _ => length > 0 ? $"VARCHAR({length})" : "VARCHAR(255)"
                },
                DbType.MySql => type switch
                {
                    FieldType.String => length > 0 ? $"VARCHAR({length})" : "LONGTEXT",
                    FieldType.Int => "INT",
                    FieldType.Long => "BIGINT",
                    FieldType.Bool => "TINYINT(1)",
                    FieldType.Decimal => "DECIMAL(18,2)",
                    FieldType.DateTime => "DATETIME",
                    FieldType.Guid => "VARCHAR(36)",
                    _ => length > 0 ? $"VARCHAR({length})" : "VARCHAR(255)"
                },
                _ => type switch
                {
                    FieldType.String => length > 0 ? $"VARCHAR({length})" : "NVARCHAR(MAX)",
                    FieldType.Int => "INT",
                    FieldType.Long => "BIGINT",
                    FieldType.Bool => "BIT",
                    FieldType.Decimal => "DECIMAL(18,2)",
                    FieldType.DateTime => "DATETIME",
                    FieldType.Guid => "UNIQUEIDENTIFIER",
                    _ => length > 0 ? $"VARCHAR({length})" : "VARCHAR(255)"
                }
            };
        }

        private static string GetDbTypeName(FieldType type, DbType dbType)
        {
            return dbType switch
            {
                DbType.PostgreSQL => type switch
                {
                    FieldType.String => "varchar",
                    FieldType.Int => "int",
                    FieldType.Long => "bigint",
                    FieldType.Bool => "bool",
                    FieldType.Decimal => "numeric",
                    FieldType.DateTime => "timestamp",
                    FieldType.Guid => "uuid",
                    _ => "varchar"
                },
                DbType.MySql => type switch
                {
                    FieldType.String => "varchar",
                    FieldType.Int => "int",
                    FieldType.Long => "bigint",
                    FieldType.Bool => "tinyint",
                    FieldType.Decimal => "decimal",
                    FieldType.DateTime => "datetime",
                    FieldType.Guid => "varchar",
                    _ => "varchar"
                },
                _ => type switch
                {
                    FieldType.String => "varchar",
                    FieldType.Int => "int",
                    FieldType.Long => "bigint",
                    FieldType.Bool => "bit",
                    FieldType.Decimal => "decimal",
                    FieldType.DateTime => "datetime",
                    FieldType.Guid => "uniqueidentifier",
                    _ => "varchar"
                }
            };
        }

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
