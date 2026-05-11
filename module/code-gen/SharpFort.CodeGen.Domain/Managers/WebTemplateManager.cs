using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SqlSugar;
using Volo.Abp.Domain.Services;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CodeGen.Domain.Managers
{
    /// <summary>
    /// 与webfrist相关，同步到web，code to web
    /// </summary>
    public class WebTemplateManager(ISqlSugarRepository<Table> repository, IModuleContainer moduleContainer) : DomainService
    {
        private readonly ISqlSugarRepository<Table> _repository = repository;
        private readonly IModuleContainer _moduleContainer = moduleContainer;

        /// <summary>
        /// 通过当前的实体代码获取表存储
        /// </summary>
        /// <returns></returns>

        public Task<List<Table>> BuildCodeToWebAsync()
        {
            List<Type> entityTypes = [];
            foreach (IAbpModuleDescriptor module in _moduleContainer.Modules)
            {
                entityTypes.AddRange(module.Assembly.GetTypes()
                    .Where(x => x.GetCustomAttribute<IgnoreCodeFirstAttribute>() == null)
                    .Where(x => x.GetCustomAttribute<SugarTable>() != null)
                    .Where(x => x.GetCustomAttribute<SplitTableAttribute>() is null));
            }

            List<Table> tables = [];
            foreach (Type entityType in entityTypes)
            {
                tables.Add(EntityTypeMapperToTable(entityType));
            }

            return Task.FromResult(tables);
        }

        private static Table EntityTypeMapperToTable(Type entityType)
        {
            Table table = new()
            {
                Fields = []
            };
            SugarTable? sugarTable = entityType.GetCustomAttribute<SugarTable>();

            table.Name = sugarTable?.TableName ?? entityType.Name;

            foreach (PropertyInfo p in entityType.GetProperties())
            {
                table.Fields.Add(PropertyMapperToFiled(p));
            }
            table.Fields.ForEach(x => x.TableId = table.Id);
            return table;
        }


        private static Field PropertyMapperToFiled(PropertyInfo propertyInfo)
        {
            Field fieldEntity = new()
            {
                Name = propertyInfo.Name
            };


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
            string? enumName = typeof(FieldType).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.GetCustomAttribute<DisplayAttribute>()?.Description == fieldType.Name).FirstOrDefault()?.Name;
            if (enumName is null)
            {
                fieldEntity.FieldType = FieldType.String;
                // App.GetRequiredService<ILogger<WebTemplateManager>>().LogError($"字段类型：{propertyInfo.PropertyType.Name}，未定义");
            }
            else
            {
                fieldEntity.FieldType = Enum.Parse<FieldType>(enumName);
            }

            //判断是否可空
            if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                fieldEntity.IsRequired = false;
            }
            else
            {
                fieldEntity.IsRequired = true;
            }



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
