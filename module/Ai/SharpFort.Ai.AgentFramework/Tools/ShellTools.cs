using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SharpFort.Ai.AgentFramework.Tools;

public static class ShellTools
{
    private static readonly string[] DangerousPatterns =
    [
        "rm -rf /", "rm -rf /*",
        "sudo ", "shutdown", "reboot",
        "> /dev/", ":(){ :|:& };:",
        "mkfs.", "dd if=", "format ",
        "del /f /s /q",
    ];

    [Description("执行 Shell 命令")]
    public static string RunShell(
        [Description("要执行的 Shell 命令")] string command,
        [Description("命令执行的工作目录（可选）")] string? workingDirectory = null)
    {
        try
        {
            if (DangerousPatterns.Any(d => command.Contains(d, StringComparison.OrdinalIgnoreCase)))
                return "❌ 安全拦截：检测到危险命令，已阻止执行。";

            var isWindows = OperatingSystem.IsWindows();
            var processInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd" : "bash",
                Arguments = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory) && Directory.Exists(workingDirectory))
                processInfo.WorkingDirectory = workingDirectory;

            using var process = Process.Start(processInfo);
            if (process == null) return "❌ 无法启动 Shell 进程";

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(60_000))
            {
                process.Kill(entireProcessTree: true);
                return "❌ 命令执行超时（60秒），已强制终止。";
            }

            var result = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(stdout)) result.AppendLine(stdout.Trim());
            if (!string.IsNullOrWhiteSpace(stderr)) result.AppendLine($"⚠️ stderr: {stderr.Trim()}");
            if (process.ExitCode != 0) result.AppendLine($"⚠️ 退出码: {process.ExitCode}");

            var output = result.Length > 0 ? result.ToString() : "(命令执行成功，无输出)";

            const int maxOutputLength = 50_000;
            if (output.Length > maxOutputLength)
                output = output[..maxOutputLength] + "\n... (输出已截断，超过 50KB 上限)";

            return output;
        }
        catch (Exception ex) { return $"❌ 执行失败: {ex.Message}"; }
    }
}
