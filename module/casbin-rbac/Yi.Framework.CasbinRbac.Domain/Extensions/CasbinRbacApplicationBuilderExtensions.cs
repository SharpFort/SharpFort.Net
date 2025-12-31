using Microsoft.AspNetCore.Builder;
using Yi.Framework.CasbinRbac.Domain.Authorization;

namespace Yi.Framework.CasbinRbac.Domain.Extensions
{
    public static class CasbinRbacApplicationBuilderExtensions
    {
        /// <summary>
        /// 启用 Casbin RBAC 鉴权中间件
        /// 请确保在 UseAuthentication 之后，UseAuthorization 之前调用
        /// </summary>
        public static IApplicationBuilder UseCasbinRbac(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CasbinAuthorizationMiddleware>();
        }
    }
}
