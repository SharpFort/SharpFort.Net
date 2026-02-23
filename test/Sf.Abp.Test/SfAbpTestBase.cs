using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;


namespace Sf.Abp.Test
{
    public class SfAbpTestBase :AbpTestBaseWithServiceProvider
    {
        public ILogger Logger { get; private set; }
        protected IServiceScope TestServiceScope { get; }
        public SfAbpTestBase()
        {
            IHost host = Host.CreateDefaultBuilder()
               .UseAutofac()
               .ConfigureServices((host, service) =>
               {
                   ConfigureServices(host, service);
                   service.AddLogging(builder => builder.ClearProviders().AddConsole().AddDebug());
                   /*application= */
                   service.AddApplicationAsync<SfAbpTestModule>().Wait();
               })
               .ConfigureAppConfiguration(this.ConfigureAppConfiguration)
               .Build();

            this.ServiceProvider = host.Services;
            this.TestServiceScope = ServiceProvider.CreateScope();
            this.Logger = (ILogger)this.ServiceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(this.GetType()));

            host.InitializeAsync().Wait();
        }


        public virtual void ConfigureServices(HostBuilderContext host, IServiceCollection service)
        { 
        }
        protected virtual void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.AddJsonFile("appsettings.json");
            configurationBuilder.AddJsonFile("appsettings.Development.json");
            
        }
    }
}
