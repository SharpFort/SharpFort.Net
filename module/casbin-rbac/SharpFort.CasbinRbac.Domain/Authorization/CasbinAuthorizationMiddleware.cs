using System.IdentityModel.Tokens.Jwt;
using Casbin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using SharpFort.CasbinRbac.Domain.Shared.Options;

namespace SharpFort.CasbinRbac.Domain.Authorization
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
        private readonly IJwtBlacklist _jwtBlacklist;

        // P-04: 预处理 IgnoreUrls — 精确匹配 O(1) + 前缀匹配 O(m)
        private readonly HashSet<string> _exactIgnoreUrls;
        private readonly List<string> _prefixIgnoreUrls;

        public CasbinAuthorizationMiddleware(
            ICurrentUser currentUser,
            ICurrentTenant currentTenant,
            IEnforcer enforcer,
            IOptions<CasbinOptions> options,
            IJwtBlacklist jwtBlacklist)
        {
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _enforcer = enforcer;
            _options = options.Value;
            _jwtBlacklist = jwtBlacklist;

            _exactIgnoreUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _prefixIgnoreUrls = [];

            if (_options.IgnoreUrls != null)
            {
                foreach (string url in _options.IgnoreUrls)
                {
                    if (url.StartsWith("exact:", StringComparison.OrdinalIgnoreCase))
                    {
                        _exactIgnoreUrls.Add(url[6..]);
                    }
                    else
                    {
                        _prefixIgnoreUrls.Add(url);
                    }
                }
            }
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            string? path = context.Request.Path.Value;

            // 0. P-04: IgnoreUrls 检查 — O(1) 精确 + O(m) 前缀
            if (!string.IsNullOrEmpty(path) && (_exactIgnoreUrls.Count + _prefixIgnoreUrls.Count > 0))
            {
                if (_exactIgnoreUrls.Contains(path))
                {
                    await next(context);
                    return;
                }
                foreach (string prefix in _prefixIgnoreUrls)
                {
                    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        await next(context);
                        return;
                    }
                }
            }

            // 1. Whitelist / AllowAnonymous checks
            Endpoint? endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await next(context);
                return;
            }

            // 1.5 S-07: JWT 黑名单检查（在认证检查之前）
            if (_currentUser.IsAuthenticated)
            {
                string? jti = _currentUser.FindClaim(JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti) && _jwtBlacklist.IsRevoked(jti))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["X-Token-Revoked"] = "true";
                    return;
                }
            }

            // 2. Identity (sub)
            if (!_currentUser.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            string? sub = _currentUser.Id?.ToString();

            // 3. Domain (dom)
            string dom = _currentTenant.Id?.ToString() ?? "default";

            // 4. Resource (obj)
            // B-06: 统一转小写以确保 keyMatch2 匹配一致性
            // API 路径在 ASP.NET Core 中大小写不敏感，但 Casbin keyMatch2 大小写敏感
            string? obj = path?.ToLowerInvariant();

            // 5. Action (act)
            string act = context.Request.Method.ToUpperInvariant();

            // 6. F-08: 超管快速路径 — 即使 *,* 策略丢失也能保证 admin 不被锁死
            // 这是策略层的应急备份，与 casbin_rule 中的 *,* 职责互补
            string? adminRoleCode = _options.SuperAdminRoleCode;
            if (!string.IsNullOrEmpty(adminRoleCode))
            {
                var userRoles = _enforcer.GetRolesForUser(sub, dom);
                if (userRoles.Contains(adminRoleCode))
                {
                    await next(context);
                    return;
                }
            }

            // 7. Enforce
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
