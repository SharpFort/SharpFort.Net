using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpFort.Ai.AgentFramework.Tools;

/// <summary>
/// 常用工具类
/// </summary>
public static class CommonTools
{
    [Description("获取当前系统，返回运行时平台的友好名称")]
    public static string GetRuntimePlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "MacOS";
        return "Unknown";
    }

    [Description("获取当前系统桌面路径")]
    public static string GetDesktopPath(
        [Description("ensureExists 默认为 true 则确保目录存在")] bool ensureExists = true)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
                desktop = Path.Combine(home, "Desktop");
        }
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Directory.GetCurrentDirectory();
        if (ensureExists)
        {
            try { Directory.CreateDirectory(desktop); } catch { }
        }
        return desktop;
    }

    [Description("输出文件到系统桌面")]
    public static string WriteTextToDesktop(
        [Description("文件名称如（xx.html,xx.txt）支持各种文件类型")] string fileName,
        [Description("内容")] string content,
        [Description("文件是否不存在 默认是")] bool overwrite = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "❌ 保存失败: fileName 不能为空。";
            var encoding = new UTF8Encoding(false);
            string desktop = GetDesktopPath(true);
            string safeName = MakeSafeFileName(fileName);
            string combined = Path.IsPathRooted(safeName) ? safeName : Path.Combine(desktop, safeName);
            string dir = Path.GetDirectoryName(combined) ?? desktop;
            Directory.CreateDirectory(dir);
            if (File.Exists(combined) && !overwrite) return "❌ 保存失败: 文件已存在，overwrite = false。";
            File.WriteAllText(combined, content ?? string.Empty, encoding);
            return combined;
        }
        catch (Exception ex) { return $"❌ 保存失败: {ex.Message}"; }
    }

    private static string MakeSafeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return fileName ?? string.Empty;
        char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var parts = fileName.Split(seps, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid) parts[i] = parts[i].Replace(c, '_');
        }
        return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
    }
}
