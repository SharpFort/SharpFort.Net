namespace SharpFort.CasbinRbac.Domain.Authorization
{
    /// <summary>
    /// JWT 黑名单接口 (S-07)
    /// </summary>
    public interface IJwtBlacklist
    {
        /// <summary>将 Token 加入黑名单</summary>
        void Revoke(string jti, DateTime tokenExpiresAt);

        /// <summary>检查 JTI 是否在黑名单中</summary>
        bool IsRevoked(string jti);
    }
}
