
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;


namespace SharpFort.Ai.Application.Services;

public class AiAccountService(
    IAccountService accountService,
    ISqlSugarRepository<ChatMessage> messageRepository) : ApplicationService
{
    private readonly IAccountService _accountService = accountService;
    private readonly ISqlSugarRepository<ChatMessage> _messageRepository = messageRepository;

    /// <summary>
    /// 获取ai用户信息
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("account/ai")]
    public async Task<UserRoleMenuDto> GetAsync()
    {
        Guid userId = CurrentUser.GetId();
        UserRoleMenuDto userAccount = await _accountService.GetAsync();
        return userAccount;
    }

}
