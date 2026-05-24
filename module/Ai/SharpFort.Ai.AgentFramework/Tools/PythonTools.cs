using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SharpFort.Ai.AgentFramework.Tools;

public static class PythonTools
{
    [Description("执行Python脚本")]
    public static string RunPythonPy(
        [Description("需要执行的python脚本路径")] string scriptPath,
        [Description("需要传入python脚本的参数")] List<string>? args = null)
    {
        try
        {
            if (!scriptPath.Contains(':'))
                scriptPath = AppContext.BaseDirectory + scriptPath.Replace("/", @"\");

            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (args is not null)
            {
                foreach (var item in args)
                    start.Arguments += $" {item}";
            }

            using var process = Process.Start(start);
            if (process == null) return "❌ 无法启动Python进程";

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error)) return $"❌ 执行失败: {error}";
            if (string.IsNullOrWhiteSpace(output)) output = "Python脚本执行完成，但没有输出结果。";
            return output;
        }
        catch (Exception ex) { return $"❌ 执行失败: {ex.Message}"; }
    }

    [Description("用于执行Python代码")]
    public static string RunPythonCode(
        [Description("需要执行的python代码")] string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code)) return "执行Py代码为空。";
            var saveResult = SavePythonToFile(code, "Pys", "");
            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{saveResult}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(start);
            if (process == null) return "❌ 无法启动Python进程";

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error)) return $"❌ 执行失败: {error}";
            if (string.IsNullOrWhiteSpace(output)) output = "Python脚本执行完成，但没有输出结果。";
            return output;
        }
        catch (Exception ex) { return $"❌ 执行失败: {ex.Message}"; }
    }

    [Description("把传入的python代码保存为.py文件")]
    public static string SavePythonToFile(string code, string relativeDir = "Skills/python-skills/tmp", string? fileName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code)) return "❌ 保存失败: 代码内容为空。";
            relativeDir = relativeDir.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            string fullDir = Path.Combine(baseDir, relativeDir);
            Directory.CreateDirectory(fullDir);

            fileName ??= $"py_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.py";
            if (!fileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                fileName += ".py";

            string fullPath = Path.Combine(fullDir, fileName);
            File.WriteAllText(fullPath, code, new UTF8Encoding(false));
            return fullPath;
        }
        catch (Exception ex) { return $"❌ 保存失败: {ex.Message}"; }
    }
}
