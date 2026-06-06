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
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Repositories;
using SharpFort.CasbinRbac.Domain.Shared.Caches;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;
using SharpFort.CasbinRbac.Domain.Shared.Etos;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    public class UserManager(
        ISqlSugarRepository<User> repository,
        ISqlSugarRepository<UserRole> repositoryUserRole,
        ISqlSugarRepository<UserPosition> repositoryUserPost,
        IGuidGenerator guidGenerator,
        IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> userCache,
        IUserRepository userRepository,
        ILocalEventBus localEventBus,
        ISqlSugarRepository<Role> roleRepository,
        ICasbinPolicyManager casbinPolicyManager) : DomainService
    {
        private readonly ISqlSugarRepository<User> _repository = repository;
        private readonly ISqlSugarRepository<UserRole> _repositoryUserRole = repositoryUserRole;
        private readonly ISqlSugarRepository<UserPosition> _repositoryUserPost = repositoryUserPost;
        private readonly ISqlSugarRepository<Role> _roleRepository = roleRepository;
        private readonly IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> _userCache = userCache;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly ILocalEventBus _localEventBus = localEventBus;
        private readonly ICasbinPolicyManager _casbinPolicyManager = casbinPolicyManager;

        /// <summary>
        /// 给用户设置角色
        /// </summary>
        public async Task GiveUserSetRoleAsync(List<Guid> userIds, List<Guid> roleIds)
        {
            try
            {
                // 1. 业务逻辑 (持久化)
                await _repositoryUserRole.DeleteAsync(u => userIds.Contains(u.UserId));

                if (roleIds is not null)
                {
                    // G4: 批量插入 — 将所有映射关系收集到一个集合，单次 InsertRangeAsync
                    List<UserRole> allUserRoleEntities = [];
                    foreach (Guid userId in userIds)
                    {
                        foreach (Guid roleId in roleIds)
                        {
                            allUserRoleEntities.Add(new UserRole() { UserId = userId, RoleId = roleId });
                        }
                    }
                    await _repositoryUserRole.InsertRangeAsync(allUserRoleEntities);
                }

                // 2. Casbin 同步逻辑
                List<User> users = await _repository.GetListAsync(u => userIds.Contains(u.Id));
                List<Role> roles = [];
                if (roleIds is not null && roleIds.Count > 0)
                {
                    roles = await _roleRepository.GetListAsync(r => roleIds.Contains(r.Id));
                }

                foreach (User user in users)
                {
                    await _casbinPolicyManager.SetUserRolesAsync(user, roles);
                }
            }
            finally
            {
                // S4: 角色变更后立即清除受影响用户的缓存
                foreach (Guid userId in userIds)
                {
                    await _userCache.RemoveAsync(new UserInfoCacheKey(userId));
                }
            }
        }


        /// <summary>
        /// 给用户设置岗位
        /// </summary>
        public async Task GiveUserSetPostAsync(List<Guid> userIds, List<Guid> postIds)
        {
            try
            {
                await _repositoryUserPost.DeleteAsync(u => userIds.Contains(u.UserId));
                if (postIds is not null)
                {
                    List<UserPosition> allUserPostEntities = [];
                    foreach (Guid userId in userIds)
                    {
                        foreach (Guid postId in postIds)
                        {
                            allUserPostEntities.Add(new UserPosition() { UserId = userId, PostId = postId });
                        }
                    }
                    await _repositoryUserPost.InsertRangeAsync(allUserPostEntities);
                }
            }
            finally
            {
                // S4: 岗位变更后清除受影响用户的缓存
                foreach (Guid userId in userIds)
                {
                    await _userCache.RemoveAsync(new UserInfoCacheKey(userId));
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

            bool isExist = await _repository.IsAnyAsync(x => x.UserName == userEntity.UserName);
            if (isExist)
            {
                throw new UserFriendlyException(UserConst.Exist);
            }

            User entity = await _repository.InsertReturnEntityAsync(userEntity);

            await _localEventBus.PublishAsync(new UserCreateEventArgs(entity.Id));
        }


        public async Task SetDefautRoleAsync(Guid userId)
        {
            Role? role = await _roleRepository.GetFirstAsync(x => x.RoleCode == UserConst.DefaultRoleCode);
            if (role is not null)
            {
                await GiveUserSetRoleAsync([userId], [role.Id]);
            }
        }

        private static void ValidateUserName(User input)
        {
            if (input.UserName is UserConst.Admin or UserConst.TenantAdmin)
            {
                throw new UserFriendlyException("用户名无效注册！");
            }

            if (input.UserName!.StartsWith(UserConst.OAuthTempPrefix, StringComparison.Ordinal))
            {
                throw new UserFriendlyException("注册账号不能以ls_字符开头");
            }

            if (input.UserName!.Length < 2)
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
            User user = await _userRepository.GetUserAllInfoAsync(userId);
            UserRoleMenuDto? data = EntityMapToDto(user);
            return data is null ? throw new AbpAuthorizationException() : data;
        }

        /// <summary>
        /// 使用 IDistributedCache（ABP 统一缓存抽象，支持 MemoryCache 和 Redis 自由切换）
        /// </summary>
        public async Task<UserRoleMenuDto> GetInfoByCacheAsync(Guid userId)
        {
            UserRoleMenuDto? output = null;
            long tokenExpiresMinuteTime = LazyServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.ExpiresMinuteTime;
            UserInfoCacheItem? cacheData = await _userCache.GetOrAddAsync(new UserInfoCacheKey(userId),
               async () =>
               {
                   User user = await _userRepository.GetUserAllInfoAsync(userId);
                   UserRoleMenuDto data = EntityMapToDto(user) ?? throw new AbpAuthorizationException();
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


        /// <summary>
        /// G3: 批量缓存查询 — 先尝试逐个命中缓存，cache miss 的统一用单次 DB 批量查询
        /// </summary>
        public async Task<List<UserRoleMenuDto>> GetInfoListAsync(List<Guid> userIds)
        {
            List<UserRoleMenuDto> result = [];

            // 第一遍：尝试从缓存获取；收集 cache miss 的 userId
            List<Guid> missedIds = [];
            foreach (Guid userId in userIds)
            {
                UserInfoCacheItem? cacheData = await _userCache.GetAsync(new UserInfoCacheKey(userId));
                if (cacheData is not null)
                {
                    result.Add(cacheData.Info);
                }
                else
                {
                    missedIds.Add(userId);
                }
            }

            // 第二遍：批量 DB 查询所有 cache miss 的用户
            if (missedIds.Count > 0)
            {
                long tokenExpiresMinuteTime = LazyServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.ExpiresMinuteTime;
                List<User> users = await _userRepository.GetListUserAllInfoAsync(missedIds);
                Dictionary<Guid, User> userDict = users.ToDictionary(u => u.Id);

                foreach (Guid userId in missedIds)
                {
                    if (userDict.TryGetValue(userId, out User? user))
                    {
                        UserRoleMenuDto dto = EntityMapToDto(user)
                            ?? throw new AbpAuthorizationException();
                        // 写回缓存
                        await _userCache.SetAsync(new UserInfoCacheKey(userId),
                            new UserInfoCacheItem(dto),
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(tokenExpiresMinuteTime) });
                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private static UserRoleMenuDto EntityMapToDto(User user)
        {
            UserRoleMenuDto userRoleMenu = new();
            if (user is null)
            {
                throw new UserFriendlyException($"数据错误，查询用户不存在，请重新登录");
            }

            //超级管理员特殊处理
            if (UserConst.Admin.Equals(user.UserName, StringComparison.Ordinal))
            {
                userRoleMenu.User = user.Adapt<UserDto>();
                userRoleMenu.User.Password = string.Empty;
                userRoleMenu.RoleCodes.Add(UserConst.AdminRolesCode);
                userRoleMenu.PermissionCodes.Add(UserConst.AdminPermissionCode);
                return userRoleMenu;
            }

            List<Role> roleList = user.Roles;

            foreach (Role role in roleList)
            {
                userRoleMenu.RoleCodes.Add(role.RoleCode!);

                if (role.Menus is not null)
                {
                    foreach (Menu menu in role.Menus)
                    {
                        if (!string.IsNullOrEmpty(menu.PermissionCode))
                        {
                            userRoleMenu.PermissionCodes.Add(menu.PermissionCode);
                        }
                        userRoleMenu.Menus.Add(menu.Adapt<MenuDto>());
                    }
                }

                role.Menus = [];
                userRoleMenu.Roles.Add(role.Adapt<RoleDto>());
            }

            user.Roles = [];
            userRoleMenu.User = user.Adapt<UserDto>();
            userRoleMenu.User.Password = string.Empty;
            userRoleMenu.Menus = [.. userRoleMenu.Menus.OrderBy(x => x.OrderNum).ThenBy(x => x.CreationTime)];
            return userRoleMenu;
        }
    }
}
