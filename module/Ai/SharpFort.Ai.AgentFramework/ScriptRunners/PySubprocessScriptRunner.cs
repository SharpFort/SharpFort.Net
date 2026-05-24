using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SharpFort.Ai.AgentFramework.ScriptRunners;

public class PySubprocessScriptRunner
{
    private readonly ILogger<PySubprocessScriptRunner> _logger;
    private readonly string _pythonPath;

    public PySubprocessScriptRunner(string pythonPath = "python")
    {
        _pythonPath = pythonPath;
    }

    public async Task<object?> RunAsync(
#pragma warning disable MAAI001
        AgentFileSkill skill,
        AgentFileSkillScript script,
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        string scriptFullPath = Path.Combine(skill.Path, script.FullPath);
        if (!File.Exists(scriptFullPath))
            throw new FileNotFoundException($"Script not found: {scriptFullPath}");

        string inputJson = JsonSerializer.Serialize(arguments.ToDictionary());

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"\"{scriptFullPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.StandardInput.WriteAsync(inputJson);
        process.StandardInput.Close();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Script execution failed: {errorBuilder}");

        string result = outputBuilder.ToString().Trim();
        try { return JsonSerializer.Deserialize<object>(result); } catch { return result; }
    }
#pragma warning restore MAAI001

    public static async Task<object?> StaticRunAsync(
#pragma warning disable MAAI001
        AgentFileSkill skill,
        AgentFileSkillScript script,
        JsonElement? arguments,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        string scriptFullPath = Path.Combine(skill.Path, script.FullPath);
        if (!File.Exists(scriptFullPath))
            throw new FileNotFoundException($"Script not found: {scriptFullPath}");

        string inputJson = JsonSerializer.Serialize(arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptFullPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.StandardInput.WriteAsync(inputJson);
        process.StandardInput.Close();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Script execution failed: {errorBuilder}");

        string result = outputBuilder.ToString().Trim();
        try { return JsonSerializer.Deserialize<object>(result); } catch { return result; }
    }
#pragma warning restore MAAI001
}
