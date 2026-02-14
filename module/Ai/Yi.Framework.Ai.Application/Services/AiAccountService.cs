using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Globalization;
using System.Text;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos;
using Yi.Framework.Ai.Domain.Entities;

using Yi.Framework.Rbac.Application.Contracts.IServices;
using Yi.Framework.Rbac.Domain.Shared.Dtos;
using Yi.Framework.SqlSugarCore.Abstractions;
using Yi.Framework.Ai.Domain.Extensions;
using Yi.Framework.Ai.Domain.Shared.Enums;

namespace Yi.Framework.Ai.Application.Services;

public class AiAccountService : ApplicationService
{
    private IAccountService _accountService;
    private ISqlSugarRepository<AiUserExtraInfoEntity> _userRepository;
    private ISqlSugarRepository<AiRecharge> _rechargeRepository;
    private ISqlSugarRepository<ChatMessage> _messageRepository;
    public AiAccountService(
        IAccountService accountService,
        ISqlSugarRepository<AiUserExtraInfoEntity> userRepository,
        ISqlSugarRepository<AiRecharge> rechargeRepository,
        ISqlSugarRepository<ChatMessage> messageRepository)
    {
        _accountService = accountService;
        _userRepository = userRepository;
        _rechargeRepository = rechargeRepository;
        _messageRepository = messageRepository;
    }

    /// <summary>
    /// 获取ai用户信息
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("account/ai")]
    public async Task<AiUserRoleMenuDto> GetAsync()
    {
        var userId = CurrentUser.GetId();
        var userAccount = await _accountService.GetAsync();
        var output = userAccount.Adapt<AiUserRoleMenuDto>();

        // 是否绑定服务号
        output.IsBindFuwuhao = await _userRepository.IsAnyAsync(x => userId == x.UserId);

        // 是否为VIP用户
        output.IsVip = CurrentUser.IsAiVip();

        // 获取VIP到期时间
        if (output.IsVip)
        {
            var recharges = await _rechargeRepository._DbQueryable
                .Where(x => x.UserId == userId && x.RechargeType == RechargeTypeEnum.Vip)
                .ToListAsync();

            if (recharges.Any())
            {
                // 如果有任何一个充值记录的过期时间为null，说明是永久VIP
                if (recharges.Any(x => !x.ExpireDateTime.HasValue))
                {
                    output.VipExpireTime = null; // 永久VIP
                }
                else
                {
                    // 取最大的过期时间
                    output.VipExpireTime = recharges
                        .Where(x => x.ExpireDateTime.HasValue)
                        .Max(x => x.ExpireDateTime);
                }
            }
        }

        return output;
    }

}
