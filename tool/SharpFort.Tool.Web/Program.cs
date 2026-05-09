using SharpFort.Tool.Web;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string selfUrl = builder.Configuration["App:SelfUrl"]
    ?? throw new InvalidOperationException("App:SelfUrl 配置缺失，请在 appsettings.json 中配置。");
builder.WebHost.UseUrls(selfUrl);
builder.Host.UseAutofac();
await builder.Services.AddApplicationAsync<SfAbpToolWebModule>();
WebApplication app = builder.Build();

await app.InitializeApplicationAsync();
await app.RunAsync();