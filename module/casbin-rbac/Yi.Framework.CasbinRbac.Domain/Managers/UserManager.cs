using System.Text.RegularExpressions;
using Mapster;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Repositories;
using Yi.Framework.CasbinRbac.Domain.Shared.Caches;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.CasbinRbac.Domain.Shared.Dtos;
using Yi.Framework.CasbinRbac.Domain.Shared.Etos;
using Yi.Framework.CasbinRbac.Domain.Shared.Options;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Domain.Managers
{
    public class UserManager : DomainService
    {
        public readonly ISqlSugarRepository<User> _repository;
        public readonly ISqlSugarRepository<UserRole> _repositoryUserRole;
        public readonly ISqlSugarRepository<UserPosition> _repositoryUserPost;
        private readonly ISqlSugarRepository<Role> _roleRepository;
        private IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> _userCache;
        private readonly IGuidGenerator _guidGenerator;
        private IUserRepository _userRepository;
        private ILocalEventBus _localEventBus;
        private readonly ICasbinPolicyManager _casbinPolicyManager; // 新增

        public UserManager(
            ISqlSugarRepository<User> repository, 
            ISqlSugarRepository<UserRole> repositoryUserRole, 
            ISqlSugarRepository<UserPosition> repositoryUserPost, 
            IGuidGenerator guidGenerator, 
            IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> userCache, 
            IUserRepository userRepository, 
            ILocalEventBus localEventBus, 
            ISqlSugarRepository<Role> roleRepository,
            ICasbinPolicyManager casbinPolicyManager) // 注入
        {
            _repository = repository;
            _repositoryUserRole = repositoryUserRole;
            _repositoryUserPost = repositoryUserPost;
            _guidGenerator = guidGenerator;
            _userCache = userCache;
            _userRepository = userRepository;
            _localEventBus = localEventBus;
            _roleRepository = roleRepository;
            _casbinPolicyManager = casbinPolicyManager;
        }

        /// <summary>
        /// 给用户设置角色
        /// </summary>
        public async Task GiveUserSetRoleAsync(List<Guid> userIds, List<Guid> roleIds)
        {
            // 1. 业务逻辑 (持久化)
            // 删除用户之前所有的用户角色关系（物理删除，没有恢复的必要）
            await _repositoryUserRole.DeleteAsync(u => userIds.Contains(u.UserId));

            if (roleIds is not null)
            {
                // 遍历用户
                foreach (var userId in userIds)
                {
                    // 添加新的关系
                    List<UserRole> userRoleEntities = new();
                    foreach (var roleId in roleIds)
                    {
                        userRoleEntities.Add(new UserRole() { UserId = userId, RoleId = roleId });
                    }
                    // 一次性批量添加
                    await _repositoryUserRole.InsertRangeAsync(userRoleEntities);
                }
            }

            // 2. Casbin 同步逻辑
            var users = await _repository.GetListAsync(u => userIds.Contains(u.Id));
            var roles = new List<Role>();
            if (roleIds is not null && roleIds.Any())
            {
                roles = await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id));
            }

            foreach (var user in users)
            {
                // 将选中的 roles 赋给该 user (Casbin g 策略)
                await _casbinPolicyManager.SetUserRolesAsync(user, roles);
            }
        }


        /// <summary>
        /// 给用户设置岗位
        /// </summary>
        public async Task GiveUserSetPostAsync(List<Guid> userIds, List<Guid> postIds)
        {
            await _repositoryUserPost.DeleteAsync(u => userIds.Contains(u.UserId));
            if (postIds is not null)
            {
                foreach (var userId in userIds)
                {
                    List<UserPosition> userPostEntities = new();
                    foreach (var post in postIds)
                    {
                        userPostEntities.Add(new UserPosition() { UserId = userId, PostId = post });
                    }
                    await _repositoryUserPost.InsertRangeAsync(userPostEntities);
                }
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task CreateAsync(User userEntity)
        {
            ValidateUserName(userEntity);

            if (userEntity.Phone is not null)
            {
                if (await _repository.IsAnyAsync(x => x.Phone == userEntity.Phone))
                {
                    throw new UserFriendlyException(UserConst.Phone_Repeat);
                }
            }

            var isExist = await _repository.IsAnyAsync(x => x.UserName == userEntity.UserName);
            if (isExist)
            {
                throw new UserFriendlyException(UserConst.Exist);
            }

            var entity = await _repository.InsertReturnEntityAsync(userEntity);

            // 发布事件 (可能会触发 SetDefautRoleAsync，进而触发 Casbin 同步)
            await _localEventBus.PublishAsync(new UserCreateEventArgs(entity.Id));
        }


        public async Task SetDefautRoleAsync(Guid userId)
        {
            var role = await _roleRepository.GetFirstAsync(x => x.RoleCode == UserConst.DefaultRoleCode);
            if (role is not null)
            {
                // 这会调用上面修改过的 GiveUserSetRoleAsync，从而自动同步 Casbin
                await GiveUserSetRoleAsync(new List<Guid> { userId }, new List<Guid> { role.Id });
            }
        }

        private void ValidateUserName(User input)
        {
            if (input.UserName == UserConst.Admin || input.UserName == UserConst.TenantAdmin)
            {
                throw new UserFriendlyException("用户名无效注册！");
            }

            if (input.UserName.Length < 2)
            {
                throw new UserFriendlyException("账号名需大于等于2位！");
            }

            string pattern = @"^[a-zA-Z0-9_]+$";

            bool isMatch = Regex.IsMatch(input.UserName, pattern);
            if (!isMatch)
            {
                throw new UserFriendlyException("用户名不能包含除【字母】与【数字】与【_】的其他字符");
            }
        }

        public async Task<UserRoleMenuDto> GetInfoAsync(Guid userId)
        {
            var user = await _userRepository.GetUserAllInfoAsync(userId);
            var data = EntityMapToDto(user);
            if (data is null)
            {
                throw new AbpAuthorizationException();
            }
            return data;
        }

        private async Task<UserRoleMenuDto> GetInfoByCacheAsync(Guid userId)
        {
            UserRoleMenuDto output = null;
            var tokenExpiresMinuteTime = LazyServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.ExpiresMinuteTime;
            var cacheData = await _userCache.GetOrAddAsync(new UserInfoCacheKey(userId),
               async () =>
               {
                   var user = await _userRepository.GetUserAllInfoAsync(userId);
                   var data = EntityMapToDto(user);
                   if (data is null)
                   {
                       throw new AbpAuthorizationException();
                   }
                   output = data;
                   return new UserInfoCacheItem(data);
               },
             () => new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(tokenExpiresMinuteTime) });

            if (cacheData is not null)
            {
                output = cacheData.Info;
            }
            return output!;
        }


        public async Task<List<UserRoleMenuDto>> GetInfoListAsync(List<Guid> userIds)
        {
            List<UserRoleMenuDto> output = new List<UserRoleMenuDto>();
            foreach (var userId in userIds)
            {
                output.Add(await GetInfoByCacheAsync(userId));
            }
            return output;
        }

        private UserRoleMenuDto EntityMapToDto(User user)
        {
            var userRoleMenu = new UserRoleMenuDto();
            if (user is null)
            {
                throw new UserFriendlyException($"数据错误，查询用户不存在，请重新登录");
            }

            //超级管理员特殊处理
            if (UserConst.Admin.Equals(user.UserName))
            {
                userRoleMenu.User = user.Adapt<UserDto>();
                userRoleMenu.User.Password = string.Empty;
                // userRoleMenu.User.Salt = string.Empty; // UserDto 可能没有 Salt 了
                userRoleMenu.RoleCodes.Add(UserConst.AdminRolesCode);
                userRoleMenu.PermissionCodes.Add(UserConst.AdminPermissionCode);
                return userRoleMenu;
            }

            var roleList = user.Roles;

            foreach (var role in roleList)
            {
                userRoleMenu.RoleCodes.Add(role.RoleCode);

                if (role.Menus is not null)
                {
                    foreach (var menu in role.Menus)
                    {
                        if (!string.IsNullOrEmpty(menu.PermissionCode))
                        {
                            userRoleMenu.PermissionCodes.Add(menu.PermissionCode);
                        }
                        userRoleMenu.Menus.Add(menu.Adapt<MenuDto>());
                    }
                }

                role.Menus = new List<Menu>();
                userRoleMenu.Roles.Add(role.Adapt<RoleDto>());
            }

            user.Roles = new List<Role>();
            userRoleMenu.User = user.Adapt<UserDto>();
            userRoleMenu.User.Password = string.Empty;
            userRoleMenu.Menus = userRoleMenu.Menus.OrderByDescending(x => x.OrderNum).ToHashSet();
            return userRoleMenu;
        }
    }
}
