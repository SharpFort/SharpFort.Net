
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Globalization;
using System.Text;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Domain.Shared.Dtos;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;


namespace SharpFort.Ai.Application.Services;

public class AiAccountService : ApplicationService
{
    private IAccountService _accountService;
    private ISqlSugarRepository<ChatMessage> _messageRepository;
    public AiAccountService(
        IAccountService accountService,
        ISqlSugarRepository<ChatMessage> messageRepository)
    {
        _accountService = accountService;
        _messageRepository = messageRepository;
    }

    /// <summary>
    /// 获取ai用户信息
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("account/ai")]
    public async Task<UserRoleMenuDto> GetAsync()
    {
        var userId = CurrentUser.GetId();
        var userAccount = await _accountService.GetAsync();
        return userAccount;
    }

}
