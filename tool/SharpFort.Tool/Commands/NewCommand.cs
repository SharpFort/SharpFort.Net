using System.Globalization;
using System.IO.Compression;
using Microsoft.Extensions.CommandLineUtils;
using SharpFort.Tool.Application.Contracts;
using SharpFort.Tool.Application.Contracts.Dtos;

namespace SharpFort.Tool.Commands
{
    public class NewCommand(ITemplateGenService templateGenService) : ICommand
    {
        private readonly ITemplateGenService _templateGenService = templateGenService;

        public string Command => "new";
        public string? Description => "创建项目模板` yi-abp new <name> -csf `";

        public void CommandLineApplication(CommandLineApplication application)
        {
            application.HelpOption("-h|--help");

            CommandOption templateTypeOption = application.Option("-t|--template", "模板类型:`module`|`porject`",
                CommandOptionType.SingleValue);
            CommandOption pathOption = application.Option("-p|--path", "创建路径", CommandOptionType.SingleValue);
            CommandOption csfOption = application.Option("-csf", "是否创建解决方案文件夹", CommandOptionType.NoValue);

            CommandOption soureOption = application.Option("-s|--soure", "模板来源，gitee模板库分支名称: 默认值`default`",
                CommandOptionType.SingleValue);

            CommandOption dbmsOption = application.Option("-dbms|--dataBaseMs", "数据库类型，支持目前主流数据库",
                CommandOptionType.SingleValue);

            CommandArgument moduleNameArgument = application.Argument("moduleName", "模块名", (_) => { });

            //子命令，new list
            application.Command("list", (applicationlist) =>
            {
                applicationlist.OnExecute(() =>
                {
                    Console.WriteLine("正在远程搜索中...");
                    List<string> list = _templateGenService.GetAllTemplatesAsync().Result;
                    string tip = $"""
                              全部模板包括:
                              模板名称
                              ----------------
                              {list.JoinAsString("\n")}
                              """;
                    Console.WriteLine(tip);
                    return 0;
                });
            });

            application.OnExecute(() =>
            {
                if (dbmsOption.HasValue())
                {
                    Console.WriteLine($"检测到使用数据库类型-{dbmsOption.Value()}，请在生成后，只需在配置文件中，更改DbConnOptions:Url及DbType即可，支持目前主流数据库20+");
                }

                string path = string.Empty;
                if (pathOption.HasValue())
                {
                    path = pathOption.Value();
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        return 0;
                    }

                }



                #region 处理生成类型

                string id = Guid.NewGuid().ToString("N");
                string zipPath = string.Empty;
                byte[] fileByteArray;

                string soure = soureOption.HasValue() ? soureOption.Value() : "default";

                string templateType = templateTypeOption.HasValue() ? templateTypeOption.Value() : "module";
                if (templateType == "module")
                {
                    //代表模块生成
                    fileByteArray = (_templateGenService.CreateModuleAsync(new TemplateGenCreateInputDto
                    {
                        Name = moduleNameArgument.Value,
                        ModuleSoure = soure
                    }).Result);
                }
                else
                {
                    //还是代表模块生成
                    fileByteArray = _templateGenService.CreateModuleAsync(new TemplateGenCreateInputDto
                    {
                        Name = moduleNameArgument.Value,
                    }).Result;
                }



                zipPath = Path.Combine(path, $"{id}.zip");
                File.WriteAllBytes(zipPath, fileByteArray);

                #endregion

                #region 处理解决方案文件夹

                //默认是当前目录
                string unzipDirPath = "./";
                //如果创建解决方案文件夹
                if (csfOption.HasValue())
                {
                    string moduleName = moduleNameArgument.Value.ToLower(CultureInfo.InvariantCulture).Replace(".", "-");

                    unzipDirPath = Path.Combine(path, moduleName);
                    if (Directory.Exists(unzipDirPath))
                    {
                        throw new UserFriendlyException($"文件夹[{unzipDirPath}]已存在，请删除后重试");
                    }

                    Directory.CreateDirectory(unzipDirPath);
                }

                #endregion


                ZipFile.ExtractToDirectory(zipPath, unzipDirPath);
                //创建压缩包后删除临时目录
                File.Delete(zipPath);

                Console.WriteLine("恭喜~模块已生成！");
                return 0;
            });
        }
    }
}