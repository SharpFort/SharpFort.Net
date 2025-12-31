using Volo.Abp.Domain.Repositories;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Shared.Dtos;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Repositories
{
    public interface IUserRepository : ISqlSugarRepository<User>
    {
        /// <summary>
        /// 获取用户的所有信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<User> GetUserAllInfoAsync(Guid userId);
        /// <summary>
        /// 批量获取用户的所有信息
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns></returns>
        Task<List<User>> GetListUserAllInfoAsync(List<Guid> userIds);

    }
}
