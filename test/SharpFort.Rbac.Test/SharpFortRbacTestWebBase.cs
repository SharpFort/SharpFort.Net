using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace SharpFort.Rbac.Test
{
    public class SharpFortRbacTestWebBase : SharpFortCasbinRbacTestBase
    {
        public HttpContext HttpContext { get; private set; }
        public SharpFortRbacTestWebBase() : base()
        {
            HttpContext httpContext = DefaultHttpContextAccessor.CurrentHttpContext!;
            ConfigureHttpContext(httpContext);
            HttpContext = httpContext;
            ApplicationBuilder app = new(ServiceProvider);
            RequestDelegate httpDelegate = app.Build();
            httpDelegate.Invoke(httpContext);
        }

        public override void ConfigureServices(HostBuilderContext host, IServiceCollection service)
        {
            service.Replace(new ServiceDescriptor(typeof(IHttpContextAccessor), typeof(DefaultHttpContextAccessor), ServiceLifetime.Singleton));
            base.ConfigureServices(host, service);
        }

        protected virtual void ConfigureHttpContext(HttpContext httpContext)
        {
            httpContext.Request.Path = "/test";
        }
    }
}
internal sealed class DefaultHttpContextAccessor : IHttpContextAccessor
{
    internal static HttpContext? CurrentHttpContext { get; set; } = new DefaultHttpContext();
    public HttpContext? HttpContext { get => CurrentHttpContext; set => throw new NotImplementedException(); }
}
