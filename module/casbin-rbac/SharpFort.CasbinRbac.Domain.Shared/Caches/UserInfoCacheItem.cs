using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;

namespace SharpFort.CasbinRbac.Domain.Shared.Caches
{
    public class UserInfoCacheItem
    {
        public UserInfoCacheItem(UserRoleMenuDto info) { Info = info; }
        /// <summary>
        /// 存储的用户信息
        /// </summary>
        public UserRoleMenuDto Info { get; set; }
    }
    public class UserInfoCacheKey
    {
        public UserInfoCacheKey(Guid userId) { UserId = userId; }

        public Guid UserId { get; set; }

        public override string ToString()
        {
            return $"User:{UserId}";
        }
    }
}
