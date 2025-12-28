using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Security.Claims;
using Yi.Framework.Core.Helper;
using Yi.Framework.Rbac.Domain.Entities;
using Yi.Framework.Rbac.Domain.Repositories;
using Yi.Framework.Rbac.Domain.Shared.Caches;
using Yi.Framework.Rbac.Domain.Shared.Consts;
using Yi.Framework.Rbac.Domain.Shared.Dtos;
using Yi.Framework.Rbac.Domain.Shared.Etos;
using Yi.Framework.Rbac.Domain.Shared.Model;
using Yi.Framework.Rbac.Domain.Shared.Options;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Rbac.Domain.Managers
{

    /// <summary>
    /// 用户领域服务
    /// </summary>
    public class AccountManager : DomainService, IAccountManager
    {
        private readonly IUserRepository _repository;
        private readonly ILocalEventBus _localEventBus;
        private readonly JwtOptions _jwtOptions;
        private readonly RbacOptions _options;
        private UserManager _userManager;
        private ISqlSugarRepository<Role> _roleRepository;
        private RefreshJwtOptions _refreshJwtOptions;

        public AccountManager(IUserRepository repository
            , IOptions<JwtOptions> jwtOptions
            , ILocalEventBus localEventBus
            , UserManager userManager
            , IOptions<RefreshJwtOptions> refreshJwtOptions
            , ISqlSugarRepository<Role> roleRepository
            , IOptions<RbacOptions> options)
        {
            _repository = repository;
            _jwtOptions = jwtOptions.Value;
            _localEventBus = localEventBus;
            _userManager = userManager;
            _roleRepository = roleRepository;
            _refreshJwtOptions = refreshJwtOptions.Value;
            _options = options.Value;
        }

        /// <summary>
        /// 根据用户id获取token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="getUserInfo"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<string> GetTokenByUserIdAsync(Guid userId,Action<UserRoleMenuDto>? getUserInfo=null)
        {
            //获取用户信息
            var userInfo = await _userManager.GetInfoAsync(userId);

            //判断用户状态
            if (userInfo.User.State == false)
            {
                throw new UserFriendlyException(UserConst.State_Is_State);
            }

            if (userInfo.RoleCodes.Count == 0)
            {
                throw new UserFriendlyException(UserConst.No_Role);
            }
            if (!userInfo.PermissionCodes.Any())
            {
                throw new UserFriendlyException(UserConst.No_Permission);
            }

            if (getUserInfo is not null)
            {
                getUserInfo(userInfo);
            }
            
            var accessToken = CreateToken(this.UserInfoToClaim(userInfo));
            //将用户信息添加到缓存中，需要考虑的是更改了用户、角色、菜单等整个体系都需要将缓存进行刷新，看具体业务进行选择
            return accessToken;
        }

        /// <summary>
        /// 创建令牌
        /// </summary>
        /// <param name="kvs"></param>
        /// <returns></returns>
        private string CreateToken(List<KeyValuePair<string, string>> kvs)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = kvs.Select(x => new Claim(x.Key, x.Value.ToString())).ToList();
            var token = new JwtSecurityToken(
               issuer: _jwtOptions.Issuer,
               audience: _jwtOptions.Audience,
               claims: claims,
               expires: DateTime.Now.AddMinutes(_jwtOptions.ExpiresMinuteTime),
               notBefore: DateTime.Now,
               signingCredentials: creds);
            string returnToken = new JwtSecurityTokenHandler().WriteToken(token);

            return returnToken;
        }

        public string CreateRefreshToken(Guid userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshJwtOptions.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            //添加用户id，及刷新token的标识
            var claims = new List<Claim> {
                new Claim(AbpClaimTypes.UserId,userId.ToString()),
                new Claim(TokenTypeConst.Refresh, "true")
            };
            var token = new JwtSecurityToken(
               issuer: _refreshJwtOptions.Issuer,
               audience: _refreshJwtOptions.Audience,
               claims: claims,
               expires: DateTime.Now.AddMinutes(_refreshJwtOptions.ExpiresMinuteTime),
               notBefore: DateTime.Now,
               signingCredentials: creds);
            string returnToken = new JwtSecurityTokenHandler().WriteToken(token);

            return returnToken;

        }
        /// <summary>
        /// 登录校验
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="userAction"></param>
        /// <returns></returns>
        public async Task LoginValidationAsync(string userName, string password, Action<User> userAction = null)
        {
            var user = new User();
            if (await ExistAsync(userName, o => user = o))
            {
                if (userAction is not null)
                {
                    userAction.Invoke(user);
                }
                // 使用新的密码验证方法（支持 BCrypt 和旧版 SHA512，并自动升级）
                if (user.VerifyAndUpgradePassword(password))
                {
                    // 如果密码被升级到 BCrypt，保存更新
                    if (user.Password.StartsWith("$2"))
                    {
                        await _repository.UpdateAsync(user);
                    }
                    return;
                }
                throw new UserFriendlyException(UserConst.Login_Error);
            }
            throw new UserFriendlyException(UserConst.Login_User_No_Exist);
        }

        /// <summary>
        /// 判断账户合法存在
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userAction"></param>
        /// <returns></returns>
        public async Task<bool> ExistAsync(string userName, Action<User> userAction = null)
        {
            var user = await _repository.GetFirstAsync(u => u.UserName == userName && u.State == true);
            if (userAction is not null)
            {
                userAction.Invoke(user);
            }
            //这里为了兼容解决数据库开启了大小写不敏感问题,还要将用户名进行二次校验
            if (user != null && user.UserName == userName)
            {
                return true;
            }
            return false;
        }
        
        

        /// <summary>
        /// 令牌转换
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        public List<KeyValuePair<string, string>> UserInfoToClaim(UserRoleMenuDto dto)
        {
            var claims = new List<KeyValuePair<string, string>>();
            AddToClaim(claims, AbpClaimTypes.UserId, dto.User.Id.ToString());
            AddToClaim(claims, AbpClaimTypes.UserName, dto.User.UserName);
            if (dto.User.DepartmentId is not null)
            {
                AddToClaim(claims, TokenTypeConst.DepartmentId, dto.User.DepartmentId.ToString());
            }
            if (dto.User.Email is not null)
            {
                AddToClaim(claims, AbpClaimTypes.Email, dto.User.Email);
            }
            if (dto.User.Phone is not null)
            {
                AddToClaim(claims, AbpClaimTypes.PhoneNumber, dto.User.Phone.ToString());
            }
            if (dto.Roles.Count > 0)
            {
                AddToClaim(claims, TokenTypeConst.RoleInfo, JsonConvert.SerializeObject(dto.Roles.Select(x => new RoleTokenInfoModel { Id = x.Id, DataScope = x.DataScope })));
            }
            if (UserConst.Admin.Equals(dto.User.UserName))
            {
                AddToClaim(claims, TokenTypeConst.Permission, UserConst.AdminPermissionCode);
                AddToClaim(claims, TokenTypeConst.Roles, UserConst.AdminRolesCode);
            }
            else
            {
                dto.PermissionCodes?.ToList()?.ForEach(per => AddToClaim(claims, TokenTypeConst.Permission, per));
                dto.RoleCodes?.ToList()?.ForEach(role => AddToClaim(claims, AbpClaimTypes.Role, role));
            }

            return claims;
        }


        private void AddToClaim(List<KeyValuePair<string, string>> claims, string key, string value)
        {
            claims.Add(new KeyValuePair<string, string>(key, value));
        }

        /// <summary>
        /// 更新密码
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newPassword"></param>
        /// <param name="oldPassword"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task UpdatePasswordAsync(Guid userId, string newPassword, string oldPassword)
        {
            var user = await _repository.GetByIdAsync(userId);

            if (!user.VerifyPassword(oldPassword))
            {
                throw new UserFriendlyException("无效更新！原密码错误！");
            }
            user.SetPassword(newPassword);
            await _repository.UpdateAsync(user);
        }

        /// <summary>
        /// 重置密码,也可以是找回密码
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> RestPasswordAsync(Guid userId, string password)
        {
            var user = await _repository.GetByIdAsync(userId);
            user.SetPassword(password);
            return await _repository.UpdateAsync(user);
        }

        /// <summary>
        /// 注册用户，创建用户之后设置默认角色
        /// </summary>
        /// <param name="userName">用户名（用于登录的唯一标识）</param>
        /// <param name="password">明文密码（将自动使用 BCrypt 加密存储）</param>
        /// <param name="phone">手机号码（可选，用于手机号登录或密码找回）</param>
        /// <param name="nick">用户昵称（可选，用于前端显示）</param>
        /// <returns>无返回值的异步任务</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 用户自主注册账户（前台注册页面）
        /// 2. 管理员批量导入用户时调用
        ///
        /// 实现逻辑：
        /// - 创建 User 实体（自动调用 BuildPassword 生成 BCrypt 哈希）
        /// - 调用 UserManager.CreateAsync 持久化用户
        /// - 调用 UserManager.SetDefautRoleAsync 分配默认角色
        ///
        /// 注意事项：
        /// - 用户名唯一性校验由 UserManager.CreateAsync 处理
        /// - 默认角色需要在 RbacOptions 中配置
        /// - 密码使用 BCrypt 加密（workFactor=12）
        /// </remarks>
        public async Task RegisterAsync( string userName, string password, long? phone,string? nick)
        {
            var user = new User(userName, password, phone,nick);
            await _userManager.CreateAsync(user);
            await _userManager.SetDefautRoleAsync(user.Id);
        }
    }

}
