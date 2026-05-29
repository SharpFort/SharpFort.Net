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
#pragma warning disable IDE0100 // 显式使用 == false 确保 SqlSugar 的 Includes 内部条件解析为 BinaryExpression 避免 UnaryExpression 解析失败
            List<User> users = await _DbQueryable
                .Where(x => userIds.Contains(x.Id))
                .Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(),
                          r => r.Menus.Where(m => m.IsDeleted == false).ToList())
                .ToListAsync();
#pragma warning restore IDE0100
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
#pragma warning disable IDE0100 // 显式使用 == false 确保 SqlSugar 的 Includes 内部条件解析为 BinaryExpression 避免 UnaryExpression 解析失败
            User user = await _DbQueryable
                .Includes(u => u.Roles.Where(r => r.IsDeleted == false).ToList(),
                          r => r.Menus.Where(m => m.IsDeleted == false).ToList())
                .InSingleAsync(userId);
#pragma warning restore IDE0100
            return user;
        }
    }
}
