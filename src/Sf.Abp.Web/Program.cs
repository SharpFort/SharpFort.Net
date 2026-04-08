using Serilog;
using Serilog.Events;
using Sf.Abp.Web;

//创建日志,可使用{SourceContext}记录
Log.Logger = new LoggerConfiguration()
    //由于后端处理请求中，前端请求已经结束，此类日志可不记录
    .Filter.ByExcluding(log =>log.Exception?.GetType() == typeof(TaskCanceledException)||log.MessageTemplate.Text.Contains("\"message\": \"A task was canceled.\""))
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
    .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.File("logs/all/log-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug))
    .WriteTo.Async(c => c.File("logs/error/errorlog-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Error))
    .WriteTo.Async(c => c.Console())
    .CreateLogger();

try
{
    Log.Information("""

         _____ _                      ______         _     _   _      _   
        / ____| |                    |  ____|       | |   | \ | |    | |  
       | (___ | |__   __ _ _ __ _ __ | |__ ___  _ __| |_  |  \| | ___| |_ 
        \___ \| '_ \ / _` | '__| '_ \|  __/ _ \| '__| __| | . ` |/ _ \ __|
        ____) | | | | (_| | |  | |_) | | | (_) | |  | |_ _| |\  |  __/ |_ 
       |_____/|_| |_|\__,_|_|  | .__/|_|  \___/|_|   \__(_)_| \_|\___|\__|
                               | |                                        
                               |_|                                                                                                                                    
   
     """);
    Log.Information("Sf框架-Abp.vNext，启动！");

    var builder = WebApplication.CreateBuilder(args);
    Log.Information($"当前主机启动环境-【{builder.Environment.EnvironmentName}】");
    Log.Information($"当前主机启动地址-【{builder.Configuration["App:SelfUrl"]}】");
    builder.WebHost.UseUrls(builder.Configuration["App:SelfUrl"]);
    builder.Host.UseAutofac();
    builder.Host.UseSerilog();
    await builder.Services.AddApplicationAsync<SfAbpWebModule>();
    var app = builder.Build();
    await app.InitializeApplicationAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sf框架-Abp.vNext，爆炸！");
}
finally
{
    Log.CloseAndFlush();
}