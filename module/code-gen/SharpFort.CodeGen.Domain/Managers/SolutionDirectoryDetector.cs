using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace SharpFort.CodeGen.Domain.Managers
{
    /// <summary>
    /// 解决方案目录自适应探测器
    /// </summary>
    public static class SolutionDirectoryDetector
    {
        private const string EnvVarName = "SF_SOLUTION_ROOT";
        private const string ConfigKey = "CodeGen:SolutionRoot";

        public static string Detect(IConfiguration? configuration = null, string? startDir = null)
        {
            startDir ??= AppDomain.CurrentDomain.BaseDirectory;
            
            // Level 1: 向上递归查找 .sln 文件
            string? slnDir = FindSolutionBySln(startDir);
            if (slnDir != null)
            {
                return slnDir;
            }

            // Level 2: 查找子目录中 .csproj 文件最多的目录
            string? csprojDir = FindSolutionByCsprojDensity(startDir);
            if (csprojDir != null)
            {
                return csprojDir;
            }

            // Level 3: 读取环境变量或配置文件
            string? envDir = Environment.GetEnvironmentVariable(EnvVarName);
            if (!string.IsNullOrWhiteSpace(envDir) && Directory.Exists(envDir))
            {
                return envDir.Trim();
            }

            if (configuration != null)
            {
                string? configDir = configuration[ConfigKey];
                if (!string.IsNullOrWhiteSpace(configDir) && Directory.Exists(configDir))
                {
                    return configDir.Trim();
                }
            }

            // 三级全部失败，抛出详尽异常
            throw new Exception(
                $"[CodeGen] 无法自动探测到解决方案的根目录。\n" +
                $"探测起点: {startDir}\n" +
                $"已尝试的探测策略:\n" +
                $"  1. 向上递归查找 .sln 文件 (未找到)\n" +
                $"  2. 查找子目录中包含最多 .csproj 文件的层级 (未找到)\n" +
                $"  3. 环境变量 '{EnvVarName}' 或配置项 '{ConfigKey}' (未配置或路径不存在)\n" +
                $"请在本地开发环境中确保存在 .sln 文件，或在运行/部署环境中配置环境变量 '{EnvVarName}'。");
        }

        private static string? FindSolutionBySln(string currentDir)
        {
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                if (dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Any())
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }

        private static string? FindSolutionByCsprojDensity(string currentDir)
        {
            // 向上回溯到某个合理的父级（比如回退最多 4 层），然后统计该父级下所有的子目录中 .csproj 数量
            var dir = new DirectoryInfo(currentDir);
            DirectoryInfo? bestCandidate = null;
            int maxCsprojCount = 0;

            for (int i = 0; i < 4 && dir != null; i++)
            {
                try
                {
                    // 统计当前目录及其子目录下所有的 csproj
                    var csprojs = dir.GetFiles("*.csproj", SearchOption.AllDirectories);
                    if (csprojs.Length > maxCsprojCount)
                    {
                        maxCsprojCount = csprojs.Length;
                        bestCandidate = dir;
                    }
                }
                catch
                {
                    // 忽略权限受限的目录
                }
                dir = dir.Parent;
            }

            // 如果找到的目录中包含超过 2 个 csproj，则判定为项目根目录
            if (bestCandidate != null && maxCsprojCount >= 2)
            {
                return bestCandidate.FullName;
            }

            return null;
        }
    }
}
