using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace SharpFort.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// 内存级 JWT 黑名单 (S-07)
    /// 使用 Timer 定期清理过期条目，避免 ConcurrentDictionary.Count O(n) 性能问题 (R-04)
    /// 注意：仅适用于单实例部署。多实例请改用 Redis。
    /// </summary>
    public class JwtBlacklist : IJwtBlacklist, ISingletonDependency, IDisposable
    {
        private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();
        private readonly Timer _cleanupTimer;

        public JwtBlacklist()
        {
            // R-04: 使用 Timer 定期清理，每 5 分钟执行一次
            _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        /// <summary>将 Token 加入黑名单</summary>
        public void Revoke(string jti, DateTime tokenExpiresAt)
        {
            _blacklist[jti] = tokenExpiresAt;
        }

        /// <summary>检查 JTI 是否在黑名单中</summary>
        public bool IsRevoked(string jti)
        {
            if (_blacklist.TryGetValue(jti, out DateTime expiresAt))
            {
                if (DateTime.UtcNow < expiresAt)
                {
                    return true;
                }
                _blacklist.TryRemove(jti, out _);
            }
            return false;
        }

        private void CleanupExpired(object? state)
        {
            DateTime now = DateTime.UtcNow;
            List<string> expiredKeys = [];

            foreach (KeyValuePair<string, DateTime> kv in _blacklist)
            {
                if (kv.Value < now)
                {
                    expiredKeys.Add(kv.Key);
                }
            }

            foreach (string key in expiredKeys)
            {
                _blacklist.TryRemove(key, out _);
            }
        }
    }
}
