using System.Threading.Tasks;
using Casbin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;

namespace Yi.Framework.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// Casbin 鉴权中间件
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
            Microsoft.Extensions.Options.IOptions<Yi.Framework.CasbinRbac.Domain.Shared.Options.CasbinOptions> options)
        {
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // 1. 获取 Endpoint
            var endpoint = context.GetEndpoint();

            // 2. 检查 AllowAnonymous
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await next(context);
                return;
            }

            // 3. 构造 Casbin 请求参数
            var sub = _currentUser.IsAuthenticated ? $"u_{_currentUser.Id}" : "anonymous";
            var dom = _currentTenant.Id?.ToString() ?? "default";
            
            // 4. 超级管理员直通 (Bypass) - 解决跨租户管理难题
            // 只要用户拥有指定的 SuperAdmin 角色，无需查 Casbin 规则，直接放行
            // 注意: CurrentUser.Roles 通常包含 Role Name/Code
            if (_currentUser.IsAuthenticated && 
                _currentUser.Roles.Contains(_options.SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }
            
            // Object: 优先读取 YiPermissionAttribute，降级使用 Normalized URL
            string obj = null;
            var permissionAttr = endpoint?.Metadata?.GetMetadata<Yi.Framework.CasbinRbac.Domain.Shared.Attributes.YiPermissionAttribute>();
            if (permissionAttr != null)
            {
                obj = permissionAttr.Code;
            }
            else
            {
                var path = context.Request.Path.Value?.ToLower()?.TrimEnd('/');
                obj = string.IsNullOrEmpty(path) ? "/" : path;
            }
            
            // Action
            var act = context.Request.Method.ToUpper();

            // 5. 执行 Casbin 鉴权
            bool allowed = await _enforcer.EnforceAsync(sub, dom, obj, act);

            // 6. 调试及诊断
            if (_options.EnableDebugMode || context.Request.Headers.ContainsKey("X-Casbin-Debug"))
            {
                context.Response.Headers["X-Casbin-Result"] = allowed.ToString();
                context.Response.Headers["X-Casbin-Sub"] = sub;
                context.Response.Headers["X-Casbin-Obj"] = obj;
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
