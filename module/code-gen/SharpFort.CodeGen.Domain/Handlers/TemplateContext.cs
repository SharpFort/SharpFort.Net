using System;
using System.Collections.Generic;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class TemplateContext
    {
        public TableInfo Table { get; set; } = null!;
        public List<FieldInfo> Fields { get; set; } = [];
        public string Module { get; set; } = string.Empty;
        public string RootNamespace { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;           // PascalCase
        public string ModelCamel { get; set; } = string.Empty;      // camelCase
        public string ModelPlural { get; set; } = string.Empty;     // 复数形式
    }

    public class TableInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string RootNamespace { get; set; } = string.Empty;
        public bool IsOverwrite { get; set; }
        public string TemplateEngine { get; set; } = "Scriban";
    }

    public class FieldInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // 存储其 FieldType 对应的字符串，如 String, Int
        public string CsharpType { get; set; } = string.Empty; // 解析后的 C# 类型，如 string, int?
        public int MaxLength { get; set; }
        public bool IsRequired { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsQueryField { get; set; }
        public int OrderNum { get; set; }
    }
}
