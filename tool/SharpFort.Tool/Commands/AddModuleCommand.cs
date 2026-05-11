using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.CommandLineUtils;

namespace SharpFort.Tool.Commands
{
    public class AddModuleCommand : ICommand
    {
        public string Command => "add-module";
        public string? Description => "将内容添加到当前解决方案` yi-abp add-module <moduleName> [-p <path>] [-s <solution>] ";
        public void CommandLineApplication(CommandLineApplication application)
        {
            application.HelpOption("-h|--help");
            CommandOption modulePathOption = application.Option("-p|--modulePath", "模块路径", CommandOptionType.SingleValue);
            CommandOption solutionOption = application.Option("-s|--solution", "解决方案路径", CommandOptionType.SingleValue);
            CommandArgument moduleNameArgument = application.Argument("moduleName", "模块名", (_) => { });
            application.OnExecute(() =>
            {
                string moduleName = moduleNameArgument.Value;

                //模块路径默认按小写规则，默认在模块路径下一层
                string modulePath = moduleName.ToLower(CultureInfo.InvariantCulture).Replace(".", "-");
                if (modulePathOption.HasValue())
                {
                    modulePath = modulePathOption.Value();
                }


                //解决方案默认在模块文件夹上一级，也可以通过s进行指定
                string slnPath = "../";

                if (solutionOption.HasValue())
                {
                    slnPath = solutionOption.Value();
                }

                CheckFirstSlnPath(slnPath);
                List<string> dotnetSlnCommandPart = ["Application", "Application.Contracts", "Domain", "Domain.Shared", "SqlSugarCore"];
                string[] paths = [.. dotnetSlnCommandPart.Select(x => Path.Combine(modulePath, $"{moduleName}.{x}"))];
                CheckPathExist(paths);

                string[] cmdCommands = [.. dotnetSlnCommandPart.Select(x => $"dotnet sln \"{slnPath}\" add \"{Path.Combine(modulePath, $"{moduleName}.{x}")}\"")];
                StartCmd(cmdCommands);

                Console.WriteLine("恭喜~模块添加成功！");
                return 0;
            });

        }

        /// <summary>
        /// 获取一个sln解决方案，多个将报错
        /// </summary>
        /// <returns></returns>
        private static string CheckFirstSlnPath(string slnPath)
        {
            string[] slnFiles = Directory.GetFiles(slnPath, "*.sln");
            if (slnFiles.Length > 1)
            {
                throw new UserFriendlyException("当前目录包含多个sln解决方案，请只保留一个");
            }
            return slnFiles.Length == 0 ? throw new UserFriendlyException("当前目录未找到sln解决方案，请检查") : slnFiles[0];
        }


        /// <summary>
        /// 执行cmd命令
        /// </summary>
        /// <param name="cmdCommands"></param>
        private static void StartCmd(params string[] cmdCommands)
        {
            ProcessStartInfo psi = new()
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            // 判断操作系统
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c chcp 65001&{string.Join("&", cmdCommands)}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "/bin/bash";
                psi.Arguments = $"-c \"{string.Join("; ", cmdCommands)}\"";
            }

            Process proc = new()
            {
                StartInfo = psi
            };

            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            Console.WriteLine(output);

            proc.WaitForExit();
        }


        /// <summary>
        /// 检查路径
        /// </summary>
        /// <param name="paths"></param>
        /// <exception cref="UserFriendlyException"></exception>
        private static void CheckPathExist(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                {
                    throw new UserFriendlyException($"路径错误，请检查你的路径，找不到：{path}");
                }
            }
        }


    }
}
