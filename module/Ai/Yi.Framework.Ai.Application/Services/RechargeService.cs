using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.Recharge;
using Yi.Framework.Ai.Application.Contracts.IServices;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Consts;
using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Rbac.Application.Contracts.IServices;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services
{
    public class RechargeService : ApplicationService, IRechargeService
    {
        private readonly ISqlSugarRepository<AiRecharge> _repository;
        private readonly ICurrentUser _currentUser;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly AiRechargeManager _aiMessageManager;

        public RechargeService(
            ISqlSugarRepository<AiRecharge> repository,
            ICurrentUser currentUser,
            IUserService userService, IRoleService roleService, AiRechargeManager aiMessageManager)
        {
            _repository = repository;
            _currentUser = currentUser;
            _userService = userService;
            _roleService = roleService;
            _aiMessageManager = aiMessageManager;
        }

        /// <summary>
        /// 查询已登录的账户充值记录（分页）
        /// </summary>
        /// <returns></returns>
        [Route("recharge/account")]
        [Authorize]
        public async Task<PagedResultDto<RechargeGetListOutput>> GetListByAccountAsync([FromQuery]RechargeGetListInput input)
        {
            var userId = CurrentUser.Id;
            RefAsync<int> total = 0;

            var entities = await _repository._DbQueryable
                .Where(x => x.UserId == userId)
                .WhereIF(input.StartTime.HasValue, x => x.CreationTime >= input.StartTime!.Value)
                .WhereIF(input.EndTime.HasValue, x => x.CreationTime <= input.EndTime!.Value)
                .WhereIF(input.IsFree == true, x => x.RechargeAmount == 0)
                .WhereIF(input.IsFree == false, x => x.RechargeAmount > 0)
                .WhereIF(input.MinRechargeAmount.HasValue, x => x.RechargeAmount >= input.MinRechargeAmount!.Value)
                .WhereIF(input.MaxRechargeAmount.HasValue, x => x.RechargeAmount <= input.MaxRechargeAmount!.Value)
                .OrderByDescending(x => x.CreationTime)
                .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

            var output = entities.Adapt<List<RechargeGetListOutput>>();
            return new PagedResultDto<RechargeGetListOutput>(total, output);
        }

        /// <summary>
        /// 给用户充值VIP
        /// </summary>
        /// <param name="input">充值输入参数</param>
        /// <returns></returns>
        [RemoteService(isEnabled:false)]
        public async Task RechargeVipAsync(RechargeCreateInput input)
        {
            // Pay functionality removed.
            throw new NotImplementedException("VIP recharge functionality is removed.");
        }

        /// <summary>
        /// 移除用户vip及角色
        /// </summary>
        [RemoteService(isEnabled: false)]
        public async Task RemoveVipRoleByExpireAsync()
        {
            // Pay functionality removed.
            // var expiredUserIds = await _aiMessageManager.RemoveVipByExpireAsync();
            // if (expiredUserIds is not null)
            // {
            //    await _roleService.RemoveUserRoleByRoleCodeAsync(expiredUserIds, AiHubConst.VipRole);
            // }
            throw new NotImplementedException("VIP functionality is removed/disabled.");
        }
    }
}
