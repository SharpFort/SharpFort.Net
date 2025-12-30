using Microsoft.AspNetCore.Builder;

namespace Yi.Framework.Bbs.Application.Extensions;

public static class BbsAccessLogExtensions
{
    public static IApplicationBuilder UseBbsAccessLog(this IApplicationBuilder app)
    {
        app.UseMiddleware<BbsAccessLogMiddleware>();
        return app;
    }
}