#nullable disable
using SqlSugar;
using Volo.Abp.DependencyInjection;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Repositories;
using SharpFort.SqlSugarCore.Abstractions;
using SharpFort.SqlSugarCore.Repositories;

namespace SharpFort.CasbinRbac.SqlSugarCore.Repositories
{
    public class UserRepository(ISugarDbContextProvider<ISqlSugarDbContext> sugarDbContextProvider) : SqlSugarRepository<User>(sugarDbContextProvider), IUserRepository, ITransientDependency
    {
        /// <summary>
        /// 获取用户ids的全部信息
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<List<User>> GetListUserAllInfoAsync(List<Guid> userIds)
        {
            List<User> users = await _DbQueryable.Where(x => userIds.Contains(x.Id)).Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(), r => r.Menus.Where(m => m.IsDeleted == false).ToList()).ToListAsync();
            return users;
        }


        /// <summary>
        /// 获取用户id的全部信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<User> GetUserAllInfoAsync(Guid userId)
        {
            //得到用户
            User user = await _DbQueryable.Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(), r => r.Menus.Where(m => m.IsDeleted == false).ToList()).InSingleAsync(userId);
            return user;
        }




    }
}
