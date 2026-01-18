using System.Threading.Tasks;
using Casbin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;

namespace Yi.Framework.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// Casbin Authorization Middleware
    /// </summary>
    public class CasbinAuthorizationMiddleware : IMiddleware, ITransientDependency
    {
        private readonly CasbinOptions _options;
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentTenant _currentTenant;
        private readonly IEnforcer _enforcer;

        public CasbinAuthorizationMiddleware(
            ICurrentUser currentUser, 
            ICurrentTenant currentTenant, 
            IEnforcer enforcer,
            IOptions<CasbinOptions> options)
        {
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var path = context.Request.Path.Value;

            // 0. Path-based whitelist for public endpoints
            if (!string.IsNullOrEmpty(path))
            {
                var pathLower = path.ToLower();
                // Allow Swagger, Hangfire, static files, and other public paths
                if (pathLower.StartsWith("/swagger") ||
                    pathLower.StartsWith("/hangfire") ||
                    pathLower.StartsWith("/api/app/wwwroot") ||
                    pathLower.StartsWith("/_framework") ||
                    pathLower.StartsWith("/_content") ||
                    pathLower == "/" ||
                    pathLower == "/favicon.ico")
                {
                    await next(context);
                    return;
                }
            }

            // 1. Whitelist / AllowAnonymous checks
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await next(context);
                return;
            }

            // 2. Identity (sub)
            if (!_currentUser.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var sub = _currentUser.Id?.ToString();
            
            // 3. Domain (dom)
            var dom = _currentTenant.Id?.ToString() ?? "default";

            // 4. Resource (obj)
            // Strictly use Request Path for RESTful RBAC
            // Normalizing path: lowercase? 
            // The DB migration uses the path as stored in Menu (ApiUrl).
            // We should ensure consistency. 
            // Let's use the raw path or normalized to lowercase. 
            // If DB stores "/api/User", and request is "/api/user", we need a match.
            // keyMatch2 is case-sensitive? 
            // Usually paths are case-insensitive in Windows but sensitive in Linux.
            // Best practice: Normalize to lowercase for comparison if the convention allows.
            var obj = path; //.ToLower(); // Decided to keep case for now, assuming DB matches registration.

            // 5. Action (act)
            var act = context.Request.Method.ToUpper();

            // 6. Enforce
            bool allowed = await _enforcer.EnforceAsync(sub, dom, obj, act);

            // Debug headers
            if (_options.EnableDebugMode)
            {
                context.Response.Headers["X-Casbin-Sub"] = sub;
                context.Response.Headers["X-Casbin-Obj"] = obj;
                context.Response.Headers["X-Casbin-Act"] = act;
                context.Response.Headers["X-Casbin-Dom"] = dom;
                context.Response.Headers["X-Casbin-Result"] = allowed.ToString();
            }

            if (allowed)
            {
                await next(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
        }
    }
}
