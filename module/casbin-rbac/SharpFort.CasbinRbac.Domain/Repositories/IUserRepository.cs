using Volo.Abp.Domain.Repositories;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Repositories
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
