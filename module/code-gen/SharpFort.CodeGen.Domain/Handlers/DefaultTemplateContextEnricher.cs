using System;
using System.Collections.Generic;
using System.Linq;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Shared.Enums;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class DefaultTemplateContextEnricher : ITemplateContextEnricher
    {
        public int Priority => 0;

        public void Enrich(TemplateContext context, Table table)
        {
            var rootNamespace = !string.IsNullOrWhiteSpace(table.RootNamespace) ? table.RootNamespace : "Sf.Abp";
            var moduleName = !string.IsNullOrWhiteSpace(table.ModuleName) ? table.ModuleName : "Rbac";

            context.RootNamespace = rootNamespace;
            context.Module = moduleName;
            context.Model = table.Name ?? string.Empty;
            context.ModelCamel = ToCamelCase(context.Model);
            context.ModelPlural = Pluralize(context.Model);

            context.Table = new TableInfo
            {
                Name = table.Name ?? string.Empty,
                Description = table.Description ?? string.Empty,
                ModuleName = moduleName,
                RootNamespace = rootNamespace,
                IsOverwrite = table.IsOverwrite
            };

            context.Fields = [];
            if (table.Fields != null)
            {
                foreach (var field in table.Fields.OrderBy(x => x.OrderNum))
                {
                    var fieldInfo = new FieldInfo
                    {
                        Name = field.Name ?? string.Empty,
                        Type = field.FieldType.ToString(),
                        MaxLength = field.Length,
                        IsRequired = field.IsRequired,
                        IsPrimaryKey = field.IsKey,
                        Description = field.Description ?? string.Empty,
                        IsQueryField = field.IsQueryField,
                        OrderNum = field.OrderNum,
                        CsharpType = GetCsharpType(field),
                        IsListDisplay = field.IsListDisplay,
                        IsFormItem = field.IsFormItem,
                        IsPublic = field.IsPublic
                    };
                    context.Fields.Add(fieldInfo);
                }
            }
        }

        private static string GetCsharpType(Field field)
        {
            string baseType = field.FieldType switch
            {
                FieldType.String => "string",
                FieldType.Int => "int",
                FieldType.Long => "long",
                FieldType.Bool => "bool",
                FieldType.Decimal => "decimal",
                FieldType.DateTime => "DateTime",
                FieldType.Guid => "Guid",
                FieldType.Float => "float",
                FieldType.Double => "double",
                _ => "string"
            };

            // 如果不是主键且不是必填，追加可空修饰符（string 视配置也可加可空）
            if (!field.IsRequired && !field.IsKey)
            {
                return baseType + "?";
            }

            return baseType;
        }

        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Length == 1) return input.ToLowerInvariant();
            return char.ToLowerInvariant(input[0]) + input[1..];
        }

        private static string Pluralize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // 极简复数规则，适合代码生成常见表名
            if (input.EndsWith("y", StringComparison.OrdinalIgnoreCase) && 
                !input.EndsWith("ay", StringComparison.OrdinalIgnoreCase) && 
                !input.EndsWith("ey", StringComparison.OrdinalIgnoreCase) && 
                !input.EndsWith("oy", StringComparison.OrdinalIgnoreCase) && 
                !input.EndsWith("uy", StringComparison.OrdinalIgnoreCase))
            {
                return input[..^1] + "ies";
            }
            if (input.EndsWith("s", StringComparison.OrdinalIgnoreCase) || 
                input.EndsWith("x", StringComparison.OrdinalIgnoreCase) || 
                input.EndsWith("ch", StringComparison.OrdinalIgnoreCase) || 
                input.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            {
                return input + "es";
            }
            return input + "s";
        }
    }
}
