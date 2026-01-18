namespace Yi.Framework.CasbinRbac.Domain.Shared.Options
{
    public class CasbinOptions
    {
        /// <summary>
        /// 超级管理员角色代码
        /// 拥有该角色的用户将跳过所有权限检查 (Bypass)
        /// 默认: "admin"
        /// </summary>
        public string SuperAdminRoleCode { get; set; } = "admin";

        /// <summary>
        /// 是否启用调试模式
        /// 启用后支持 X-Casbin-Debug 头
        /// </summary>
        public bool EnableDebugMode { get; set; } = false;
        
        /// <summary>
        /// 忽略的 URL 前缀列表 (小写)
        /// 这些 URL 将跳过 Casbin 检查
        /// </summary>
        public List<string> IgnoreUrls { get; set; } = new List<string>();

        /// <summary>
        /// 是否启用 CachedEnforcer（本地缓存，无需 Redis）
        /// 启用后可显著提升鉴权性能，缓存 Enforce() 结果
        /// 默认: true
        /// </summary>
        public bool EnableCachedEnforcer { get; set; } = true;

        /// <summary>
        /// 是否启用 Redis Watcher（分布式策略同步）
        /// 仅在多实例部署时需要启用，需要配置 Redis 连接
        /// 默认: false
        /// </summary>
        public bool EnableRedisWatcher { get; set; } = false;
    }
}
