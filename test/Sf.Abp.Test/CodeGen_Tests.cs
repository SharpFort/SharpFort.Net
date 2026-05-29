using System;
using System.IO;
using Xunit;
using SharpFort.CodeGen.Domain.Entities;
using SharpFort.CodeGen.Domain.Handlers;
using SharpFort.CodeGen.Domain.Managers;
using SharpFort.CodeGen.Domain.Shared.Enums;

namespace Sf.Abp.Test
{
    public class CodeGen_Tests
    {
        [Fact]
        public void SolutionDirectoryDetector_Should_Detect_Sln_In_Workspace()
        {
            // 在测试环境中，向上回溯应该能找到解决方案根目录的 .sln 路径
            string root = SolutionDirectoryDetector.Detect();
            Assert.NotNull(root);
            Assert.True(Directory.Exists(root));
            Assert.True(File.Exists(Path.Combine(root, "Sf.Abp.sln")));
        }

        [Fact]
        public void SolutionDirectoryDetector_Should_Fallback_To_Environment_Variable()
        {
            string fakePath = Path.Combine(Path.GetTempPath(), "FakeTestDir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fakePath);

            try
            {
                Environment.SetEnvironmentVariable("SF_SOLUTION_ROOT", fakePath);
                string detected = SolutionDirectoryDetector.Detect(null, fakePath);
                Assert.Equal(fakePath, detected);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SF_SOLUTION_ROOT", null);
                if (Directory.Exists(fakePath))
                {
                    Directory.Delete(fakePath);
                }
            }
        }

        [Fact]
        public void IncrementalCodeMerger_Should_Add_Warning_Header_For_New_File()
        {
            var merger = new IncrementalCodeMerger();
            string newContent = "public class Test {}";
            string tempFile = Path.GetTempFileName();

            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);

                string? merged = merger.Merge(tempFile, newContent);
                Assert.NotNull(merged);
                Assert.StartsWith(IncrementalCodeMerger.WarningHeader, merged);
                Assert.Contains("public class Test", merged);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void IncrementalCodeMerger_Should_Preserve_Custom_Code_Blocks()
        {
            var merger = new IncrementalCodeMerger();
            string tempFile = Path.GetTempFileName();

            string oldContent = 
                "// <sf-custom-code-start id=\"TestBlock\">\n" +
                "public void CustomMethod() { Console.WriteLine(\"Handwritten!\"); }\n" +
                "// <sf-custom-code-end>\n";

            string newTemplateContent = 
                "public class GeneratedClass {\n" +
                "    // <sf-custom-code-start id=\"TestBlock\">\n" +
                "    // Template default body\n" +
                "    // <sf-custom-code-end>\n" +
                "}";

            try
            {
                File.WriteAllText(tempFile, oldContent);

                string? merged = merger.Merge(tempFile, newTemplateContent);
                Assert.NotNull(merged);
                Assert.Contains("public void CustomMethod() { Console.WriteLine(\"Handwritten!\"); }", merged);
                Assert.DoesNotContain("Template default body", merged);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void IncrementalCodeMerger_Should_Throw_On_Mismatched_Tags()
        {
            var merger = new IncrementalCodeMerger();
            string tempFile = Path.GetTempFileName();

            string mismatchedOldContent = 
                "// <sf-custom-code-start id=\"Unclosed\">\n" +
                "public void BadMethod() {}\n"; // 没有闭合标记

            try
            {
                File.WriteAllText(tempFile, mismatchedOldContent);

                Assert.Throws<InvalidOperationException>(() => 
                {
                    merger.Merge(tempFile, "public class Sample {}");
                });
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void IncrementalCodeMerger_Should_Not_Overwrite_If_No_Tags_Exist()
        {
            var merger = new IncrementalCodeMerger();
            string tempFile = Path.GetTempFileName();

            try
            {
                // 旧文件存在，但是没有任何合并标记
                File.WriteAllText(tempFile, "public class HandWrittenClassWithoutMarkers {}");

                string? merged = merger.Merge(tempFile, "public class NewScaffoldClass {}");
                // 返回 null 触发安全保护警告，不覆盖该文件
                Assert.Null(merged);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ScribanHelperFunctions_Should_Generate_SugarColumn_Correctly()
        {
            var field = new FieldInfo
            {
                Name = "CreateTime",
                Type = "DateTime",
                CsharpType = "DateTime",
                IsPrimaryKey = false,
                IsRequired = true,
                MaxLength = 0,
                Description = "创建时间"
            };

            string sugarCol = ScribanHelperFunctions.SugarColumn(field);
            Assert.Contains("ColumnName = \"create_time\"", sugarCol);
            Assert.Contains("IsNullable = false", sugarCol);
            Assert.Contains("ColumnDescription = \"创建时间\"", sugarCol);
            Assert.DoesNotContain("IsPrimaryKey = true", sugarCol);
        }

        [Fact]
        public void ScribanHelperFunctions_Should_Generate_DefaultValues_Correctly()
        {
            var guidField = new FieldInfo { Type = "Guid", CsharpType = "Guid" };
            var stringField = new FieldInfo { Type = "String", CsharpType = "string" };
            var nullableField = new FieldInfo { Type = "Int", CsharpType = "int?" };

            Assert.Equal("Guid.Empty", ScribanHelperFunctions.DefaultValue(guidField));
            Assert.Equal("string.Empty", ScribanHelperFunctions.DefaultValue(stringField));
            Assert.Equal("null", ScribanHelperFunctions.DefaultValue(nullableField));
        }

        [Fact]
        public void SolutionDirectoryDetector_Should_Fallback_To_DotCsproj_When_No_Sln()
        {
            // 模拟 CI 环境：使用一个不包含 .sln 的临时目录，但内部有多个 .csproj 文件
            string baseDir = Path.Combine(Path.GetTempPath(), "CiTest_" + Guid.NewGuid().ToString("N"));
            string nestedDir = Path.Combine(baseDir, "src", "ProjectA");
            Directory.CreateDirectory(nestedDir);

            try
            {
                // 创建多个 .csproj 文件模拟项目结构
                File.WriteAllText(Path.Combine(nestedDir, "ProjectA.csproj"), "<Project />");
                File.WriteAllText(Path.Combine(baseDir, "src", "ProjectB.csproj"), "<Project />");
                Directory.CreateDirectory(Path.Combine(baseDir, "test"));
                File.WriteAllText(Path.Combine(baseDir, "test", "ProjectC.csproj"), "<Project />");
                // 注意：临时目录中没有 .sln 文件，触发 Fallback

                string detected = SolutionDirectoryDetector.Detect(null, nestedDir);
                Assert.NotNull(detected);

                // Fallback 应该找到包含最多 csproj 的目录
                Assert.True(Directory.Exists(detected));
            }
            finally
            {
                if (Directory.Exists(baseDir))
                {
                    Directory.Delete(baseDir, true);
                }
            }
        }

        [Fact]
        public void SolutionDirectoryDetector_Should_Throw_When_All_Levels_Fail()
        {
            // 在空目录中，无 .sln 无 .csproj，且无环境变量 → 应抛出异常
            string emptyDir = Path.Combine(Path.GetTempPath(), "EmptyDir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(emptyDir);

            try
            {
                string? oldEnv = Environment.GetEnvironmentVariable("SF_SOLUTION_ROOT");
                Environment.SetEnvironmentVariable("SF_SOLUTION_ROOT", null);
                try
                {
                    Assert.Throws<Exception>(() => SolutionDirectoryDetector.Detect(null, emptyDir));
                }
                finally
                {
                    Environment.SetEnvironmentVariable("SF_SOLUTION_ROOT", oldEnv);
                }
            }
            finally
            {
                if (Directory.Exists(emptyDir))
                {
                    Directory.Delete(emptyDir);
                }
            }
        }

        [Theory]
        [InlineData("String", false, false, "string?")]   // 非必填 string → string?
        [InlineData("String", true, false, "string")]      // 必填 string → string
        [InlineData("Int", false, false, "int?")]          // 非必填 int → int?
        [InlineData("Int", true, false, "int")]            // 必填 int → int
        [InlineData("Int", false, true, "int")]            // 主键 int → 非可空
        [InlineData("Long", false, false, "long?")]
        [InlineData("Long", true, false, "long")]
        [InlineData("Bool", false, false, "bool?")]
        [InlineData("Bool", true, false, "bool")]
        [InlineData("Decimal", false, false, "decimal?")]
        [InlineData("Decimal", true, false, "decimal")]
        [InlineData("DateTime", false, false, "DateTime?")]
        [InlineData("DateTime", true, false, "DateTime")]
        [InlineData("Guid", false, false, "Guid?")]
        [InlineData("Guid", true, false, "Guid")]
        public void DefaultTemplateContextEnricher_Should_Map_CsharpType_Correctly(
            string fieldTypeName, bool isRequired, bool isKey, string expectedCsharpType)
        {
            var fieldType = Enum.Parse<SharpFort.CodeGen.Domain.Shared.Enums.FieldType>(fieldTypeName);
            var field = new SharpFort.CodeGen.Domain.Entities.Field
            {
                Name = "TestField",
                FieldType = fieldType,
                IsRequired = isRequired,
                IsKey = isKey
            };

            var enricher = new DefaultTemplateContextEnricher();
            var context = new TemplateContext();
            var table = new Table(Guid.NewGuid(), "TestTable") { Fields = [field] };

            enricher.Enrich(context, table);

            Assert.Single(context.Fields);
            Assert.Equal(expectedCsharpType, context.Fields[0].CsharpType);
        }
    }
}
