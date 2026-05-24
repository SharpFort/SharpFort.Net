using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public static class ScribanHelperFunctions
    {
        public static string SugarColumn(FieldInfo field)
        {
            if (field == null) return string.Empty;

            var parts = new List<string>
            {
                $"ColumnName = \"{ToSnakeCase(field.Name)}\""
            };

            if (field.IsPrimaryKey)
            {
                parts.Add("IsPrimaryKey = true");
            }
            else
            {
                parts.Add($"IsNullable = {(!field.IsRequired).ToString().ToLowerInvariant()}");
            }

            if (field.MaxLength > 0)
            {
                parts.Add($"Length = {field.MaxLength}");
            }

            if (!string.IsNullOrWhiteSpace(field.Description))
            {
                parts.Add($"ColumnDescription = \"{field.Description.Replace("\"", "\\\"")}\"");
            }

            return $"[SugarColumn({string.Join(", ", parts)})]";
        }

        public static string CsharpType(FieldInfo field)
        {
            return field?.CsharpType ?? "string";
        }

        public static string DefaultValue(FieldInfo field)
        {
            if (field == null) return "null";

            if (field.CsharpType.EndsWith("?"))
            {
                return "null";
            }

            return field.Type.ToLowerInvariant() switch
            {
                "string" => "string.Empty",
                "guid" => "Guid.Empty",
                "bool" => "false",
                "int" => "0",
                "long" => "0",
                "decimal" => "0M",
                "datetime" => "DateTime.MinValue",
                _ => "null"
            };
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLowerInvariant();
        }
    }
}
