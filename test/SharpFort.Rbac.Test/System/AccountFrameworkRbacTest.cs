using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;
using SharpFort.Rbac.Application.Contracts.Dtos.Account;
using SharpFort.Rbac.Application.Contracts.Dtos.User;
using SharpFort.Rbac.Application.Contracts.IServices;
using SharpFort.Rbac.Application.Services.System;
using SharpFort.Rbac.Domain.Entities;
using SharpFort.Rbac.Domain.Shared.Consts;
using SharpFort.Rbac.Test;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Rbac.Test.System
{
    public class AccountFrameworkRbacTest : SharpFortRbacTestWebBase
    {

        private IAccountService _accountService;
        private ISqlSugarRepository<User> _userRepository;
        public AccountFrameworkRbacTest()
        {
            _accountService = GetRequiredService<IAccountService>();
            _userRepository = GetRequiredService<ISqlSugarRepository<User>>();
        }

        /// <summary>
        /// 注册
        /// </summary>
        [Fact]
        public async Task Register_Test()
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
        public async Task Register_UserNameRepeat_Error_Test()
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
        public async Task Register_PhoneRepeat_Error_Test()
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
        public async Task Login_Test()
        {
            await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "LoginTest", Password = "123456", Phone = 13845645645 });
            var result = await _accountService.PostLoginAsync(new LoginInputVo { UserName = "LoginTest", Password = "123456" });

            result.GetType().GetProperty("Token").GetValue(result, null).ToString().ShouldNotBeNull();
            result.GetType().GetProperty("RefreshToken").GetValue(result, null).ToString().ShouldNotBeNull();
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Reset_Passworld_Test()
        {
            await _accountService.PostRegisterAsync(new RegisterDto() { UserName = "ResetPassworldTest", Password = "123456", Phone = 15945645555 });
            var user = await _userRepository._DbQueryable.Where(user => user.UserName == "ResetPassworldTest").FirstAsync();
            await _accountService.RestPasswordAsync(user.Id, new RestPasswordDto { Password = "654321abc" });
            var result = await _accountService.PostLoginAsync(new LoginInputVo { UserName = "ResetPassworldTest", Password = "654321abc" });

        }
    }
}
