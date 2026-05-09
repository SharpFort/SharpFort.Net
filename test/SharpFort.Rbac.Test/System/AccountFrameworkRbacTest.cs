using Shouldly;
using Xunit;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Rbac.Test.System
{
    public class AccountFrameworkRbacTest : SharpFortRbacTestWebBase
    {

        private readonly IAccountService _accountService;
        private readonly ISqlSugarRepository<User> _userRepository;
        public AccountFrameworkRbacTest()
        {
            _accountService = GetRequiredService<IAccountService>();
            _userRepository = GetRequiredService<ISqlSugarRepository<User>>();
        }

        /// <summary>
        /// 注册
        /// </summary>
        [Fact]
        public async Task RegisterTest()
        {
            await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "RegisterTest", Password = "123456", Phone = 15945645645 });
            var user = await _userRepository._DbQueryable.Where(user => user.UserName == "RegisterTest").FirstAsync();
            user.ShouldNotBeNull();
            user.VerifyPassword("123456").ShouldBeTrue();
        }

        /// <summary>
        /// 用户名重复注册
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RegisterUserNameRepeatErrorTest()
        {
            try
            {
                await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "RegisterUserNameRepeatErrorTest", Password = "123456", Phone = 15945645641 });
                await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "RegisterUserNameRepeatErrorTest", Password = "123456", Phone = 15945645642 });
            }
            catch (UserFriendlyException ex)
            {
                ex.Message.ShouldBe(UserConst.Exist);
            }
        }

        /// <summary>
        /// 电话号码重复注册
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RegisterPhoneRepeatErrorTest()
        {
            try
            {
                await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "RegisterPhoneRepeatErrorTest1", Password = "123456", Phone = 15945645633 });
                await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "RegisterPhoneRepeatErrorTest2", Password = "123456", Phone = 15945645633 });
            }
            catch (UserFriendlyException ex)
            {
                ex.Message.ShouldBe(UserConst.Phone_Repeat);
            }
        }


        /// <summary>
        /// 登录测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task LoginTest()
        {
            await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "LoginTest", Password = "123456", Phone = 13845645645 });
            var result = await _accountService.PostLoginAsync(new LoginInputVo { UserName = "LoginTest", Password = "123456" });

            result.GetType().GetProperty("Token")!.GetValue(result, null)!.ToString().ShouldNotBeNull();
            result.GetType().GetProperty("RefreshToken")!.GetValue(result, null)!.ToString().ShouldNotBeNull();
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ResetPasswordTest()
        {
            await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "ResetPassworldTest", Password = "123456", Phone = 15945645555 });
            var user = await _userRepository._DbQueryable.Where(user => user.UserName == "ResetPassworldTest").FirstAsync();
            await _accountService.RestPasswordAsync(user.Id, new RestPasswordDto { Password = "654321abc" });
            var result = await _accountService.PostLoginAsync(new LoginInputVo { UserName = "ResetPassworldTest", Password = "654321abc" });

        }
    }
}
