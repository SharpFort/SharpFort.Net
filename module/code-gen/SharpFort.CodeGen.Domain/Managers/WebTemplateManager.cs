using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.Domain.Services;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Domain.Managers
{
    /// <summary>
    /// 实体注册表管理器 (Code→Web)
    /// 扫描 C# 实体类并同步到 SfTable 注册表，采用增量合并策略
    /// </summary>
    public class WebTemplateManager(
        ISqlSugarRepository<Table> tableRepository,
        ISqlSugarRepository<Field> fieldRepository,
        IModuleContainer moduleContainer,
        ILogger<WebTemplateManager> logger) : DomainService
    {
        private readonly ISqlSugarRepository<Table> _tableRepository = tableRepository;
        private readonly ISqlSugarRepository<Field> _fieldRepository = fieldRepository;
        private readonly IModuleContainer _moduleContainer = moduleContainer;
        private readonly ILogger<WebTemplateManager> _logger = logger;

        /// <summary>
        /// 扫描所有实体类并增量合并到注册表 (Upsert by Name)
        /// </summary>
        public async Task<List<Table>> BuildCodeToWebAsync()
        {
            List<Type> entityTypes = [];
            foreach (IAbpModuleDescriptor module in _moduleContainer.Modules)
            {
                entityTypes.AddRange(module.Assembly.GetTypes()
                    .Where(x => x.GetCustomAttribute<IgnoreCodeFirstAttribute>() == null)
                    .Where(x => x.GetCustomAttribute<SugarTable>() != null)
                    .Where(x => x.GetCustomAttribute<SplitTableAttribute>() is null));
            }

            List<Table> scannedTables = [];
            foreach (Type entityType in entityTypes)
            {
                Table table = EntityTypeMapperToTable(entityType);
                table.ProjectName = ExtractProjectName(entityType.Namespace);
                table.LastSyncTime = DateTime.UtcNow;
                scannedTables.Add(table);
            }

            // 增量合并 (Upsert by Name)
            foreach (var scanned in scannedTables)
            {
                var existing = await _tableRepository._DbQueryable
                    .Includes(x => x.Fields)
                    .Where(x => x.Name == scanned.Name)
                    .FirstAsync();

                if (existing != null)
                {
                    // 更新已有记录：保留用户手动配置，仅更新结构性字段
                    existing.Description = scanned.Description ?? existing.Description;
                    existing.PhysicalTableName = scanned.PhysicalTableName;
                    existing.ProjectName = scanned.ProjectName;
                    existing.LastSyncTime = scanned.LastSyncTime;

                    // 合并字段列表：保留已有字段的 UI 配置，新增/更新结构字段
                    MergeFields(existing, scanned);

                    await _tableRepository.UpdateAsync(existing);
                    // 同步字段到数据库
                    await _fieldRepository._Db.Deleteable<Field>().Where(f => f.TableId == existing.Id).ExecuteCommandAsync();
                    if (existing.Fields.Count > 0)
                    {
                        await _fieldRepository._Db.Insertable(existing.Fields).ExecuteCommandAsync();
                    }
                }
                else
                {
                    // 插入新记录
                    scanned.Fields.ForEach(x =>
                    {
                        x.IsQueryField = true;
                        x.IsListDisplay = true;
                        x.IsFormItem = true;
                        x.HtmlType = "Input";
                    });
                    await _tableRepository._Db.InsertNav(scanned).Include(x => x.Fields).ExecuteCommandAsync();
                }
            }

            _logger.LogInformation("[CodeGen] 实体注册表同步完成，共处理 {Count} 个实体", scannedTables.Count);
            return scannedTables;
        }

        /// <summary>
        /// 从命名空间提取项目名称
        /// 例: "SharpFort.Rbac.Domain.Entities" → "Rbac"
        /// </summary>
        private static string? ExtractProjectName(string? namespaceStr)
        {
            if (string.IsNullOrEmpty(namespaceStr)) return null;
            var parts = namespaceStr.Split('.');
            return parts.Length >= 2 ? parts[1] : parts[0];
        }

        /// <summary>
        /// 合并字段列表：保留已有字段的 UI 配置，更新结构性字段
        /// </summary>
        private static void MergeFields(Table existing, Table scanned)
        {
            var existingFieldMap = existing.Fields.ToDictionary(f => f.Name, f => f);
            var mergedFields = new List<Field>();

            foreach (var scannedField in scanned.Fields)
            {
                if (existingFieldMap.TryGetValue(scannedField.Name, out var existingField))
                {
                    // 保留已有的 UI 配置
                    scannedField.IsQueryField = existingField.IsQueryField;
                    scannedField.IsListDisplay = existingField.IsListDisplay;
                    scannedField.IsFormItem = existingField.IsFormItem;
                    scannedField.HtmlType = existingField.HtmlType;
                    scannedField.OrderNum = existingField.OrderNum;
                    scannedField.IsPublic = existingField.IsPublic;
                    scannedField.Description = existingField.Description ?? scannedField.Description;
                }

                scannedField.TableId = existing.Id;
                mergedFields.Add(scannedField);
            }

            existing.Fields = mergedFields;
        }

        private static Table EntityTypeMapperToTable(Type entityType)
        {
            Table table = new()
            {
                Fields = []
            };
            SugarTable? sugarTable = entityType.GetCustomAttribute<SugarTable>();

            table.Name = entityType.Name;
            table.PhysicalTableName = sugarTable?.TableName;
            table.Description = sugarTable?.TableDescription;

            int order = 0;
            foreach (PropertyInfo p in entityType.GetProperties())
            {
                table.Fields.Add(PropertyMapperToFiled(p, order++, entityType));
            }
            table.Fields.ForEach(x => x.TableId = table.Id);
            return table;
        }


        private static Field PropertyMapperToFiled(PropertyInfo propertyInfo, int order, Type entityType)
        {
            Field fieldEntity = new()
            {
                Name = propertyInfo.Name,
                OrderNum = order,
                IsQueryField = true,
                IsListDisplay = true,
                IsFormItem = true,
                HtmlType = "Input"
            };

            // 自动检测基类/审计字段：DeclaringType 不是当前实体类 → 继承自基类
            fieldEntity.IsPublic = propertyInfo.DeclaringType != entityType;


            //获取数据类型，包括可空类型
            Type? fieldType = null;
            // 如果字段类型是 Nullable<T> 泛型类型
            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type nullableType = Nullable.GetUnderlyingType(propertyInfo.PropertyType)!;
                fieldType = nullableType;
            }
            else
            {
                fieldType = propertyInfo.PropertyType;
            }

            //判断类型
            string? enumName = typeof(FieldType).GetFields(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(x => x.GetCustomAttribute<DisplayAttribute>()?.Description == fieldType.Name)?.Name;
            fieldEntity.FieldType = enumName is null ? FieldType.String : Enum.Parse<FieldType>(enumName);

            //判断是否可空
            fieldEntity.IsRequired = !propertyInfo.PropertyType.IsGenericType || propertyInfo.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>);



            //判断是否主键
            if (propertyInfo.GetCustomAttribute<SugarColumn>()?.IsPrimaryKey == true)
            {
                fieldEntity.IsKey = true;
            }

            //判断长度
            SugarColumn? colum = propertyInfo.GetCustomAttribute<SugarColumn>();
            if (colum is not null && colum.Length != 0)
            {
                fieldEntity.Length = colum.Length;
            }
            return fieldEntity;
        }
    }
}
