using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Yi.Framework.Core.Helper
{
    /// <summary>
    /// Shell 命令执行辅助类
    /// </summary>
    /// <remarks>
    /// ⚠️ 安全警告：
    /// 此类存在命令注入风险，请遵循以下安全准则：
    ///
    /// 1. 禁止事项：
    ///    - 绝对禁止将用户输入直接传递给 Bash() 或 Cmd() 方法
    ///    - 禁止拼接用户输入构造命令字符串
    ///    - 禁止在生产环境暴露执行任意命令的 API
    ///
    /// 2. 允许的使用场景：
    ///    - 仅使用硬编码的系统监控命令（如当前 ComputerHelper 的用法）
    ///    - 内部运维工具（需要严格的访问控制）
    ///
    /// 3. 如需执行带参数的命令，请使用参数化方式：
    ///    <code>
    ///    // ❌ 危险：命令注入
    ///    ShellHelper.Bash($"ls {userInput}");
    ///
    ///    // ✅ 安全：参数化执行
    ///    var process = new Process();
    ///    process.StartInfo.FileName = "ls";
    ///    process.StartInfo.ArgumentList.Add(userInput); // 自动转义
    ///    </code>
    ///
    /// 当前使用情况：仅 ComputerHelper.cs 使用硬编码命令，风险可控
    /// </remarks>
    public class ShellHelper
    {
        /// <summary>
        /// Linux 系统命令执行
        /// </summary>
        /// <param name="command">要执行的 bash 命令</param>
        /// <returns>命令标准输出</returns>
        /// <remarks>
        /// ⚠️ 安全警告：此方法存在命令注入风险
        ///
        /// 风险说明：
        /// - 仅转义双引号，不防范其他 shell 元字符
        /// - 攻击者可通过 `cmd`、$(cmd)、;cmd、|cmd 等绕过
        ///
        /// 安全使用：
        /// - 仅传入硬编码命令，绝不传入用户输入
        /// - 如需用户参数，请使用 Process.StartInfo.ArgumentList
        /// </remarks>
        public static string Bash(string command)
        {
            var escapedArgs = command.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Dispose();
            return result;
        }

        /// <summary>
        /// Windows 系统命令执行
        /// </summary>
        /// <param name="fileName">可执行文件名（如 wmic、cmd）</param>
        /// <param name="args">命令参数</param>
        /// <returns>命令标准输出</returns>
        /// <remarks>
        /// ⚠️ 安全警告：此方法存在命令注入风险
        ///
        /// 风险说明：
        /// - 无输入验证，直接传递给进程
        /// - 可能被注入恶意参数
        ///
        /// 安全使用：
        /// - 仅传入硬编码参数，绝不传入用户输入
        /// - 如需用户参数，请使用 Process.StartInfo.ArgumentList
        /// </remarks>
        public static string Cmd(string fileName, string args)
        {
            string output = string.Empty;

            var info = new ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = args;
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }
            return output;
        }
    }
}
