using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpFort.CodeGen.Domain.Managers
{
    public class IncrementalCodeMerger
    {
        private static readonly Regex StartTagRegex = new Regex(
            @"//\s*<sf-custom-code-start(?:\s+id=""([^""]+)"")?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EndTagRegex = new Regex(
            @"//\s*<sf-custom-code-end>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public const string WarningHeader = 
            "// <sf-generated-warning>此文件由代码生成器自动生成，请勿手动修改。</sf-generated-warning>\n";

        /// <summary>
        /// 合并新旧文件内容，若旧文件无标记则不覆盖。
        /// </summary>
        /// <param name="existingFilePath">已存在的文件路径</param>
        /// <param name="newContent">新渲染生成的完整内容</param>
        /// <returns>合并后的内容，或者如果不需要覆盖则返回 null 并触发警告</returns>
        public string? Merge(string existingFilePath, string newContent)
        {
            if (!File.Exists(existingFilePath))
            {
                // 新文件，直接加上警告头部
                return EnsureWarningHeader(newContent);
            }

            string oldContent = File.ReadAllText(existingFilePath);

            // 检查旧文件的标记成对性
            ValidateMarkers(oldContent, existingFilePath);
            // 检查新内容的标记成对性
            ValidateMarkers(newContent, "New Template Content");

            // 提取旧文件中的所有自定义代码块
            var customBlocks = ExtractCustomBlocks(oldContent);

            if (customBlocks.Count == 0)
            {
                // 补充 3：已存在文件但不含任何标记 -> 不覆盖，输出警告
                return null;
            }

            // 用旧内容替换新内容中的对应部分
            string mergedContent = ApplyCustomBlocks(newContent, customBlocks);
            return EnsureWarningHeader(mergedContent);
        }

        private string EnsureWarningHeader(string content)
        {
            if (content.StartsWith("// <sf-generated-warning>"))
            {
                return content;
            }
            return WarningHeader + content;
        }

        /// <summary>
        /// 校验标记的完整性，确保成对出现。
        /// </summary>
        public void ValidateMarkers(string content, string sourceName)
        {
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int startCount = 0;
            int endCount = 0;
            var openStarts = new Stack<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (StartTagRegex.IsMatch(line))
                {
                    startCount++;
                    openStarts.Push(i + 1); // 1-based line number
                }
                else if (EndTagRegex.IsMatch(line))
                {
                    endCount++;
                    if (openStarts.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"[CodeGen] 合并校验失败：在 '{sourceName}' 的第 {i + 1} 行发现了闭合标记 </sf-custom-code-end>，但没有对应的开始标记。");
                    }
                    openStarts.Pop();
                }
            }

            if (openStarts.Count > 0)
            {
                int unmatchedLine = openStarts.Pop();
                throw new InvalidOperationException(
                    $"[CodeGen] 合并校验失败：在 '{sourceName}' 的第 {unmatchedLine} 行发现了未闭合的开始标记 <sf-custom-code-start>。");
            }

            if (startCount != endCount)
            {
                throw new InvalidOperationException(
                    $"[CodeGen] 合并校验失败：标记数量不匹配。在 '{sourceName}' 中：Start 标记共 {startCount} 个，End 标记共 {endCount} 个。");
            }
        }

        private Dictionary<string, string> ExtractCustomBlocks(string content)
        {
            var customBlocks = new Dictionary<string, string>();
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            string? currentId = null;
            int anonymousCount = 0;
            StringBuilder? blockBuilder = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var startMatch = StartTagRegex.Match(line);

                if (startMatch.Success)
                {
                    currentId = startMatch.Groups[1].Value;
                    if (string.IsNullOrEmpty(currentId))
                    {
                        currentId = $"__anon_{anonymousCount++}";
                    }
                    blockBuilder = new StringBuilder();
                }
                else if (EndTagRegex.IsMatch(line))
                {
                    if (currentId != null && blockBuilder != null)
                    {
                        customBlocks[currentId] = blockBuilder.ToString().TrimEnd('\r', '\n');
                        currentId = null;
                        blockBuilder = null;
                    }
                }
                else if (blockBuilder != null)
                {
                    blockBuilder.AppendLine(line);
                }
            }

            return customBlocks;
        }

        private string ApplyCustomBlocks(string templateContent, Dictionary<string, string> customBlocks)
        {
            string[] lines = templateContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var outputLines = new List<string>();

            string? currentId = null;
            int anonymousCount = 0;
            bool skippingBody = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var startMatch = StartTagRegex.Match(line);

                if (startMatch.Success)
                {
                    outputLines.Add(line); // 保留 Start 标记

                    currentId = startMatch.Groups[1].Value;
                    if (string.IsNullOrEmpty(currentId))
                    {
                        currentId = $"__anon_{anonymousCount++}";
                    }

                    // 写入原来保留的自定义内容
                    if (customBlocks.TryGetValue(currentId, out string? preservedContent))
                    {
                        outputLines.Add(preservedContent);
                    }
                    
                    skippingBody = true; // 开始跳过模板里的默认体
                }
                else if (EndTagRegex.IsMatch(line))
                {
                    outputLines.Add(line); // 保留 End 标记
                    skippingBody = false;  // 结束跳过
                    currentId = null;
                }
                else if (!skippingBody)
                {
                    outputLines.Add(line);
                }
            }

            return string.Join(Environment.NewLine, outputLines);
        }
    }
}
