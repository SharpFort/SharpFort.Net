using Casbin;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using System.Globalization;
using System.IO;
using SqlSugar;
using TencentCloud.Tcr.V20190924.Models;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using SharpFort.Ddd.Application;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.User;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Role;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Post;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Entities;
using SharpFort.CasbinRbac.Domain.Managers;
using SharpFort.CasbinRbac.Domain.Repositories;
using SharpFort.CasbinRbac.Domain.Shared.Caches;
using SharpFort.CasbinRbac.Domain.Shared.Consts;
using SharpFort.CasbinRbac.Domain.Shared.Etos;
using SharpFort.CasbinRbac.Domain.Shared.Enums;
using SharpFort.CasbinRbac.Domain.Shared.OperLog;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// User服务实现
    /// </summary>
    public class UserService : SfCrudAppService<User, UserGetOutputDto, UserGetListOutputDto, Guid,
        UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>, IUserService
    //IUserService
    {
        protected ILocalEventBus LocalEventBus => LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

        public UserService(ISqlSugarRepository<User, Guid> repository, UserManager userManager,
            IUserRepository userRepository, ICurrentUser currentUser, IDeptService deptService,
            ILocalEventBus localEventBus,
            IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> userCache, IEnforcer enforcer) : base(repository)
            =>
                (_userManager, _userRepository, _currentUser, _deptService, _repository, _localEventBus, _enforcer) =
                (userManager, userRepository, currentUser, deptService, repository, localEventBus, enforcer);

        private UserManager _userManager { get; set; }
        private ISqlSugarRepository<User, Guid> _repository;
        private IUserRepository _userRepository { get; set; }
        private IDeptService _deptService { get; set; }

        private ICurrentUser _currentUser { get; set; }

        private ILocalEventBus _localEventBus;
        private readonly IEnforcer _enforcer;

        /// <summary>
        /// 批量查询用户
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<PagedResultDto<UserGetListOutputDto>> GetListAsync(UserGetListInputVo input)
        {
            RefAsync<int> total = 0;
            List<Guid>? deptIds = null;
            if (input.DepartmentId is not null)
            {
                deptIds = await _deptService.GetChildListAsync(input.DepartmentId ?? Guid.Empty);
            }


            List<Guid>? ids = input.Ids?.Split(",").Select(x => Guid.Parse(x)).ToList();
            var outPut = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.UserName),
                    x => x.UserName.Contains(input.UserName!))
                .WhereIF(input.Phone is not null, x => x.Phone!.Value.ToString(CultureInfo.InvariantCulture).Contains(input.Phone!.Value.ToString(CultureInfo.InvariantCulture)))
                .WhereIF(!string.IsNullOrEmpty(input.Name), x => x.Name!.Contains(input.Name!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .WhereIF(input.StartTime is not null && input.EndTime is not null,
                    x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)

                //这个为过滤当前部门，加入数据权限后，将由数据权限控制
                .WhereIF(input.DepartmentId is not null, x => deptIds!.Contains(x.DepartmentId ?? Guid.Empty))
                .WhereIF(ids is not null, x => ids!.Contains(x.Id))
                .LeftJoin<Department>((user, dept) => user.DepartmentId == dept.Id)
                .OrderByDescending(user => user.CreationTime)
                .Select((user, dept) => new UserGetListOutputDto(), true)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            var userIds = outPut.Select(x => x.Id).ToList();
            if (userIds.Count > 0)
            {
                var usersWithRelations = await _repository._DbQueryable
                    .Includes(u => u.Roles)
                    .Includes(u => u.Posts)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var dto in outPut)
                {
                    var userEntity = usersWithRelations.FirstOrDefault(u => u.Id == dto.Id);
                    if (userEntity != null)
                    {
                        dto.Roles = ObjectMapper.Map<List<Role>, List<RoleGetListOutputDto>>(userEntity.Roles);
                        dto.Posts = ObjectMapper.Map<List<Position>, List<PostGetListOutputDto>>(userEntity.Posts);
                    }
                }
            }

            var result = new PagedResultDto<UserGetListOutputDto>();
            result.Items = outPut;
            result.TotalCount = total;
            return result;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("添加用户", OperationType.Insert)]
        public async override Task<UserGetOutputDto> CreateAsync(UserCreateInputVo input)
        {
            var entitiy = await MapToEntityAsync(input);

            // 处理密码加密（与 UpdateAsync 保持一致的逻辑）
            var password = string.IsNullOrEmpty(input.Password) ? "123456" : input.Password;
            entitiy.SetPassword(password);

            await _userManager.CreateAsync(entitiy);
            await _userManager.GiveUserSetRoleAsync(new List<Guid> { entitiy.Id }, input.RoleIds ?? new List<Guid>());
            await _userManager.GiveUserSetPostAsync(new List<Guid> { entitiy.Id }, input.PostIds ?? new List<Guid>());

            // 同步 Casbin 用户角色关系 (g)
            // 需要获取角色编码，这里假设 RoleIds 对应角色的 Code 需要查询
            // 或者暂时使用 RoleId 作为角色标识（推荐使用 ID 以保持一致性，但 Casbin 通常用 Code 更可读）
            // 方案书V1.2: sub 传递 UserId, g = 用户, 角色, 域
            
            // 查询角色信息
            // 考虑事务一致性，这里应该在同一个 UOW 中
            
            // 注意：RoleService/Manager 应该提供获取角色 Code 的方法
            // 这里为了演示，假设 RoleIds 对应的角色列表需要被查询出来
            // 暂时先跳过 Role Code 查询，直接用 RoleId，但最佳实践是 RoleCode
            // 修正：Casbin g策略通常是 g, user_id, role_code, domain_id
            
            // Casbin 同步逻辑放入 GiveUserSetRoleAsync 或者在这里显式调用
            // 建议：封装一个私有方法或领域服务处理 Casbin 同步
            await SyncCasbinUserRoles(entitiy.Id, input.RoleIds ?? new List<Guid>());

            var result = await MapToGetOutputDtoAsync(entitiy);
            return result;
        }

        private async Task SyncCasbinUserRoles(Guid userId, List<Guid> roleIds)
        {
            if (roleIds.Count == 0) return;

            // 获取当前用户的域 (假设单域，或者从 User.DepartmentId 推导，或者默认 "default")
            // 方案 V1.2: g = _, _, _ (user, role, domain)
            string domain = "default"; // 默认域

            // 查询实际的 RoleCode
            var roles = await _repository._Db.Queryable<Role>().In(roleIds).ToListAsync();
            var roleCodes = roles.Select(r => r.RoleCode).ToList();

            var policies = roleCodes.Select(roleCode => new [] { userId.ToString(), roleCode, domain }).ToList();

            // 必须禁用 AutoSave 并手动 Save，因为在外层事务中
            // 已在 DI 中全局禁用 AutoSave

            await _enforcer.AddGroupingPoliciesAsync(policies);
            await _enforcer.SavePolicyAsync();
        }

        protected override async Task<User> MapToEntityAsync(UserCreateInputVo createInput)
        {
            // 使用基类的映射逻辑
            var entitiy = await base.MapToEntityAsync(createInput);
            // 注意：此时密码是明文，会在 CreateAsync 中调用 SetPassword 加密
            return entitiy;
        }

        /// <summary>
        /// 单查
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<UserGetOutputDto> GetAsync(Guid id)
        {
            //使用导航树形查询
            var entity = await _repository._DbQueryable.Includes(u => u.Roles).Includes(u => u.Posts)
                .Includes(u => u.Dept).InSingleAsync(id);

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("更新用户", OperationType.Update)]
        public async override Task<UserGetOutputDto> UpdateAsync(Guid id, UserUpdateInputVo input)
        {
            if (input.UserName == UserConst.Admin || input.UserName == UserConst.TenantAdmin)
            {
                throw new UserFriendlyException(UserConst.Name_Not_Allowed);
            }

            if (await _repository.IsAnyAsync(u => input.UserName!.Equals(u.UserName, StringComparison.Ordinal) && !id.Equals(u.Id)))
            {
                throw new UserFriendlyException(UserConst.Exist);
            }

            var entity = await _repository.GetByIdAsync(id);
            //更新密码，特殊处理
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                entity.SetPassword(input.Password);
            }

            await MapToEntityAsync(input, entity);

            var res1 = await _repository.UpdateAsync(entity);
            await _userManager.GiveUserSetRoleAsync(new List<Guid> { id }, input.RoleIds ?? new List<Guid>());
            await _userManager.GiveUserSetPostAsync(new List<Guid> { id }, input.PostIds ?? new List<Guid>());

            // Casbin 同步：更新用户角色
            // 先删除旧的
            await _enforcer.RemoveFilteredGroupingPolicyAsync(0, id.ToString());
            // 再添加新的
            await SyncCasbinUserRoles(id, input.RoleIds ?? new List<Guid>());

            return await MapToGetOutputDtoAsync(entity);
        }

        /// <summary>
        /// 更新个人中心
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [OperLog("更新个人信息", OperationType.Update)]
        public async Task<UserGetOutputDto> UpdateProfileAsync(ProfileUpdateInputVo input)
        {
            var entity = await _repository.GetByIdAsync(_currentUser.Id.GetValueOrDefault());
            ObjectMapper.Map(input, entity);

            await _repository.UpdateAsync(entity);
            var dto = await MapToGetOutputDtoAsync(entity);

            return dto;
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [Route("user/{id}/{state}")]
        [OperLog("更新用户状态", OperationType.Update)]
        public async Task<UserGetOutputDto> UpdateStateAsync([FromRoute] Guid id, [FromRoute] bool state)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null)
            {
                throw new UserFriendlyException("用户未存在");
            }

            entity.State = state;
            await _repository.UpdateAsync(entity);
            return await MapToGetOutputDtoAsync(entity);
        }

        [OperLog("删除用户", OperationType.Delete)]
        public override async Task DeleteAsync(Guid id)
        {
            // Casbin 同步：删除用户关联
            await _enforcer.RemoveFilteredGroupingPolicyAsync(0, id.ToString());
            await _enforcer.SavePolicyAsync();

            await base.DeleteAsync(id);
        }

        /// <summary>
        /// 导出 Excel（优化版本：解决冗余列和集合序列化问题）
        /// </summary>
        public override async Task<IActionResult> GetExportExcelAsync(UserGetListInputVo input)
        {
            // 1. 获取包含关联关系的数据（复用已有的分页查询逻辑，但获取全部数据）
            input.SkipCount = 0;
            input.MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount;
            var listResult = await GetListAsync(input);

            // 2. 将数据映射为专用的导出 DTO，处理“性别”、“角色”、“岗位”等字段的展示格式
            var exportData = listResult.Items.Select(x => new UserExportOutputDto
            {
                UserName = x.UserName,
                Name = x.Name,
                Nick = x.Nick,
                Gender = x.Gender switch
                {
                    Gender.Male => "男",
                    Gender.Female => "女",
                    _ => "未知"
                },
                DeptName = x.DeptName,
                // 关键点：将 List 集合扁平化为逗号分隔的字符串
                RoleNames = x.Roles != null ? string.Join(", ", x.Roles.Select(r => r.RoleName)) : "",
                PostNames = x.Posts != null ? string.Join(", ", x.Posts.Select(p => p.PostName)) : "",
                Phone = x.Phone,
                Email = x.Email,
                State = x.State ? "启用" : "禁用",
                Remark = x.Remark,
                CreationTime = x.CreationTime
            }).ToList();

            // 3. 生成 Excel 文件
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            var fileName = $"User_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.xlsx";
            var filePath = Path.Combine(tempPath, fileName);

            // MiniExcel 会根据 UserExportOutputDto 上的特性自动处理表头和格式
            await MiniExcel.SaveAsAsync(filePath, exportData);

            return new PhysicalFileResult(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public override Task PostImportExcelAsync(List<UserCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
    }
}
