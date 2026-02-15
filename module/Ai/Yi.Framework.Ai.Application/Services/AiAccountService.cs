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


namespace Yi.Framework.Ai.Application.Services;

public class AiAccountService : ApplicationService
{
    private IAccountService _accountService;
    private ISqlSugarRepository<AiUserExtraInfoEntity> _userRepository;
    private ISqlSugarRepository<ChatMessage> _messageRepository;
    public AiAccountService(
        IAccountService accountService,
        ISqlSugarRepository<AiUserExtraInfoEntity> userRepository,
        ISqlSugarRepository<ChatMessage> messageRepository)
    {
        _accountService = accountService;
        _userRepository = userRepository;
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

        return output;
    }

}
