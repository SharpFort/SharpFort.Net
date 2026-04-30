using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TencentCloud.Ame.V20190916.Models;
using TencentCloud.Tiw.V20190919.Models;
using Volo.Abp.Domain.Repositories;
using Xunit;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.User;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Rbac.Test.System
{
    public class UserFrameworkRbacTest : SharpFortCasbinRbacTestBase
    {
        private IUserService _userService;
        private ISqlSugarRepository<User> _repository;
        public UserFrameworkRbacTest()
        {
            _userService = ServiceProvider.GetRequiredService<IUserService>();
            _repository = ServiceProvider.GetRequiredService<ISqlSugarRepository<User>>();
        }

        /// <summary>
        /// 查询用户
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetUserTest()
        {
            var user = await _userService.GetListAsync(new UserGetListInputVo { UserName = UserConst.Admin });
            user.ShouldNotBeNull();
        }


        /// <summary>
        /// 创建用户
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateUserTest()
        {
            await _userService.CreateAsync(new UserCreateInputVo { UserName = "CreateUserTest", Password = "654321" });
            var user = await _userService.GetListAsync(new UserGetListInputVo { UserName = "CreateUserTest" });
            user.ShouldNotBeNull();
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateUserTest()
        {
            var createdUser = await _userService.CreateAsync(new UserCreateInputVo { Nick = "nickTest", Gender = Gender.Female, UserName = "UpdateUserTest", Password = "654321" });
            await _userService.UpdateAsync(createdUser.Id, new UserUpdateInputVo { Nick = "nickTest2", Gender = Gender.Female, UserName = "UpdateUserTest", Password = "123456888abc" });
            var user = await _repository._DbQueryable.Where(user => user.UserName == "UpdateUserTest").FirstAsync();
            user.ShouldNotBeNull();
            user.Nick.ShouldBe("nickTest2");
            user.Gender.ShouldBe(Gender.Female);
            user.VerifyPassword("123456888abc").ShouldBeTrue();
        }


        /// <summary>
        /// 删除用户
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DeleteUserTest()
        {
            var createdUser = await _userService.CreateAsync(new UserCreateInputVo { UserName = "DeleteUserTest", Password = "123456" });

            var user1 = await _repository._DbQueryable.Where(user => user.UserName == "DeleteUserTest").FirstAsync();
            user1.ShouldNotBeNull();

            await _userService.DeleteAsync(new List<Guid> { createdUser.Id });
            var user2 = await _repository._DbQueryable.Where(user => user.UserName == "DeleteUserTest").FirstAsync();
            user2.ShouldBeNull();
        }
    }
}
