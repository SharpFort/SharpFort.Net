using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using TencentCloud.Tcr.V20190924.Models;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using Yi.Framework.Bbs.Domain.Shared.Enums;
using Yi.Framework.Bbs.Domain.Shared.Etos;
using Yi.Framework.Ddd.Application;
using Yi.Framework.CasbinRbac.Application.Contracts.Dtos.User;
using Yi.Framework.CasbinRbac.Application.Contracts.IServices;
using Yi.Framework.CasbinRbac.Domain.Entities;
using Yi.Framework.CasbinRbac.Domain.Managers;
using Yi.Framework.CasbinRbac.Domain.Repositories;
using Yi.Framework.CasbinRbac.Domain.Shared.Caches;
using Yi.Framework.CasbinRbac.Domain.Shared.Consts;
using Yi.Framework.CasbinRbac.Domain.Shared.Etos;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;
using Yi.Framework.CasbinRbac.Domain.Shared.OperLog;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.CasbinRbac.Application.Services.System
{
    /// <summary>
    /// User服务实现
    /// </summary>
    public class UserService : YiCrudAppService<User, UserGetOutputDto, UserGetListOutputDto, Guid,
        UserGetListInputVo, UserCreateInputVo, UserUpdateInputVo>, IUserService
    //IUserService
    {
        protected ILocalEventBus LocalEventBus => LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

        public UserService(ISqlSugarRepository<User, Guid> repository, UserManager userManager,
            IUserRepository userRepository, ICurrentUser currentUser, IDeptService deptService,
            ILocalEventBus localEventBus,
            IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> userCache) : base(repository)
            =>
                (_userManager, _userRepository, _currentUser, _deptService, _repository, _localEventBus) =
                (userManager, userRepository, currentUser, deptService, repository, localEventBus);

        private UserManager _userManager { get; set; }
        private ISqlSugarRepository<User, Guid> _repository;
        private IUserRepository _userRepository { get; set; }
        private IDeptService _deptService { get; set; }

        private ICurrentUser _currentUser { get; set; }

        private ILocalEventBus _localEventBus;

        /// <summary>
        /// 查询用户
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<PagedResultDto<UserGetListOutputDto>> GetListAsync(UserGetListInputVo input)
        {
            RefAsync<int> total = 0;
            List<Guid> deptIds = null;
            if (input.DepartmentId is not null)
            {
                deptIds = await _deptService.GetChildListAsync(input.DepartmentId ?? Guid.Empty);
            }


            List<Guid> ids = input.Ids?.Split(",").Select(x => Guid.Parse(x)).ToList();
            var outPut = await _repository._DbQueryable.WhereIF(!string.IsNullOrEmpty(input.UserName),
                    x => x.UserName.Contains(input.UserName!))
                .WhereIF(input.Phone is not null, x => x.Phone.ToString()!.Contains(input.Phone.ToString()!))
                .WhereIF(!string.IsNullOrEmpty(input.Name), x => x.Name!.Contains(input.Name!))
                .WhereIF(input.State is not null, x => x.State == input.State)
                .WhereIF(input.StartTime is not null && input.EndTime is not null,
                    x => x.CreationTime >= input.StartTime && x.CreationTime <= input.EndTime)

                //这个为过滤当前部门，加入数据权限后，将由数据权限控制
                .WhereIF(input.DepartmentId is not null, x => deptIds.Contains(x.DepartmentId ?? Guid.Empty))
                .WhereIF(ids is not null, x => ids.Contains(x.Id))
                .LeftJoin<Department>((user, dept) => user.DepartmentId == dept.Id)
                .OrderByDescending(user => user.CreationTime)
                .Select((user, dept) => new UserGetListOutputDto(), true)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

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
            await _userManager.GiveUserSetRoleAsync(new List<Guid> { entitiy.Id }, input.RoleIds);
            await _userManager.GiveUserSetPostAsync(new List<Guid> { entitiy.Id }, input.PostIds);

            var result = await MapToGetOutputDtoAsync(entitiy);
            return result;
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

            if (await _repository.IsAnyAsync(u => input.UserName!.Equals(u.UserName) && !id.Equals(u.Id)))
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
            await _userManager.GiveUserSetRoleAsync(new List<Guid> { id }, input.RoleIds);
            await _userManager.GiveUserSetPostAsync(new List<Guid> { id }, input.PostIds);
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
            var entity = await _repository.GetByIdAsync(_currentUser.Id);
            ObjectMapper.Map(input, entity);

            await _repository.UpdateAsync(entity);
            var dto = await MapToGetOutputDtoAsync(entity);
            //发布更新昵称任务事件
            if (input.Nick != entity.Icon)
            {
                await this.LocalEventBus.PublishAsync(
                    new AssignmentEventArgs(AssignmentRequirements.UpdateNick, _currentUser.GetId(), input.Nick),
                    false);
            }

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
                throw new ApplicationException("用户未存在");
            }

            entity.State = state;
            await _repository.UpdateAsync(entity);
            return await MapToGetOutputDtoAsync(entity);
        }

        [OperLog("删除用户", OperationType.Delete)]
        public override async Task DeleteAsync(Guid id)
        {
            await base.DeleteAsync(id);
        }

        public override Task<IActionResult> GetExportExcelAsync(UserGetListInputVo input)
        {
            return base.GetExportExcelAsync(input);
        }

        public override Task PostImportExcelAsync(List<UserCreateInputVo> input)
        {
            return base.PostImportExcelAsync(input);
        }
    }
}
