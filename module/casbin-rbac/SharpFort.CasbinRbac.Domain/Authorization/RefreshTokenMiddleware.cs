using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Shared.Consts;

namespace SharpFort.CasbinRbac.Domain.Authorization
{
    [DebuggerStepThrough]
    public class RefreshTokenMiddleware : IMiddleware, ITransientDependency
    {
        private AccountManager _accountManager;
        public RefreshTokenMiddleware(AccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var refreshToken = context.Request.Headers["refresh_token"].ToString();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var authResult = await context.AuthenticateAsync(TokenTypeConst.Refresh);
                if (authResult.Succeeded)
                {
                    // authResult.Principal 在 Succeeded == true 时非 null，FindFirst 在 JWT 标准 claim 中也非 null
                    var userId = Guid.Parse(authResult.Principal!.FindFirst(AbpClaimTypes.UserId)!.Value);
                    var access_Token = await _accountManager.GetTokenByUserIdAsync(userId);
                    var refresh_Token = _accountManager.CreateRefreshToken(userId);
                    context.Response.Headers["access_token"] = access_Token;
                    context.Response.Headers["refresh_token"] = refresh_Token;
                    context.Request.Headers["Authorization"] = "Bearer " + access_Token;
                }
            }
            await next(context);
        }
    }


    public static class RefreshTokenExtensions
    {
        public static IApplicationBuilder UseRefreshToken([NotNull] this IApplicationBuilder app)
        {
            app.UseMiddleware<RefreshTokenMiddleware>();
            return app;

        }
    }

}
