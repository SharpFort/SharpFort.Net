using System.Threading.Tasks;
using Casbin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Yi.Framework.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// Casbin 鉴权中间件
    /// </summary>
    public class CasbinAuthorizationMiddleware : IMiddleware, ITransientDependency
    {
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentTenant _currentTenant;
        private readonly IEnforcer _enforcer;

        public CasbinAuthorizationMiddleware(
            ICurrentUser currentUser, 
            ICurrentTenant currentTenant, 
            IEnforcer enforcer)
        {
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // 1. 获取 Endpoint (需要 UseRouting 之后)
            var endpoint = context.GetEndpoint();

            // 2. 检查 AllowAnonymous 特性
            // 如果接口标记了 [AllowAnonymous]，则跳过 Casbin 检查
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await next(context);
                return;
            }

            // 3. 构造 Casbin 请求参数
            // Subject: 用户ID (u_GUID) 或 anonymous (未登录)
            var sub = _currentUser.IsAuthenticated ? $"u_{_currentUser.Id}" : "anonymous";
            
            // Domain: 租户ID (GUID) 或 default (无租户/宿主)
            var dom = _currentTenant.Id?.ToString() ?? "default";
            
            // Object: 请求路径 (URL)
            var obj = context.Request.Path.Value;
            
            // Action: 请求方法 (GET, POST...)
            var act = context.Request.Method;

            // 4. 执行 Casbin 鉴权
            if (await _enforcer.EnforceAsync(sub, dom, obj, act))
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
