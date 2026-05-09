using SharpFort.CasbinRbac.Domain.Shared.Dtos;

namespace SharpFort.CasbinRbac.Domain.Shared.Caches
{
    public class UserInfoCacheItem(UserRoleMenuDto info)
    {
        /// <summary>
        /// 存储的用户信息
        /// </summary>
        public UserRoleMenuDto Info { get; set; } = info;
    }
    public class UserInfoCacheKey(Guid userId)
    {
        public Guid UserId { get; set; } = userId;
        public override string ToString()
        {
            return $"User:{UserId}";
        }
    }
}
