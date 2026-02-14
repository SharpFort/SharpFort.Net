using Dm.util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos.Token;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.Ai.Domain.Managers;
using Yi.Framework.Ai.Domain.Shared.Consts;
using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

/// <summary>
/// Token服务
/// </summary>
[Authorize]
public class TokenService : ApplicationService
{
    private readonly ISqlSugarRepository<Token> _tokenRepository;
    private readonly ISqlSugarRepository<AiUsage> _usageStatisticsRepository;
    private readonly ModelManager _modelManager;

    public TokenService(
        ISqlSugarRepository<Token> tokenRepository,
        ISqlSugarRepository<AiUsage> usageStatisticsRepository,
        ModelManager modelManager)
    {
        _tokenRepository = tokenRepository;
        _usageStatisticsRepository = usageStatisticsRepository;
        _modelManager = modelManager;
    }

    /// <summary>
    /// 获取当前用户的Token列表
    /// </summary>
    [HttpGet("token/list")]
    public async Task<PagedResultDto<TokenGetListOutputDto>> GetListAsync([FromQuery] PagedAllResultRequestDto input)
    {
        RefAsync<int> total = 0;
        var userId = CurrentUser.GetId();

        var tokens = await _tokenRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);

        if (!tokens.Any())
        {
            return new PagedResultDto<TokenGetListOutputDto>();
        }



        var result = tokens.Select(t =>
        {

            return new TokenGetListOutputDto
            {
                Id = t.Id,
                Name = t.Name,
                ApiKey = t.TokenKey,
                ExpireTime = t.ExpireTime,
                IsDisabled = t.IsDisabled,
                IsEnableLog = t.IsEnableLog,
                CreationTime = t.CreationTime
            };
        }).ToList();

        return new PagedResultDto<TokenGetListOutputDto>(total, result);
    }

    [HttpGet("token/select-list")]
    public async Task<List<TokenSelectListOutputDto>> GetSelectListAsync([FromQuery] bool? includeDefault = true)
    {
        var userId = CurrentUser.GetId();
        var tokens = await _tokenRepository._DbQueryable
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsDisabled)
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new TokenSelectListOutputDto
            {
                TokenId = x.Id,
                Name = x.Name,
                IsDisabled = x.IsDisabled
            }).ToListAsync();

        if (includeDefault == true)
        {
            tokens.Insert(0, new TokenSelectListOutputDto
            {
                TokenId = Guid.Empty,
                Name = "默认",
                IsDisabled = false
            });
        }

        return tokens;
    }


    /// <summary>
    /// 创建Token
    /// </summary>
    [HttpPost("token")]
    public async Task<TokenGetListOutputDto> CreateAsync([FromBody] TokenCreateInput input)
    {
        var userId = CurrentUser.GetId();



        // 检查名称是否重复
        var exists = await _tokenRepository._DbQueryable
            .AnyAsync(x => x.UserId == userId && x.Name == input.Name);
        if (exists)
        {
            throw new UserFriendlyException($"名称【{input.Name}】已存在，请使用其他名称");
        }

        var token = new Token(userId, input.Name)
        {
            ExpireTime = input.ExpireTime
        };

        await _tokenRepository.InsertAsync(token);

        return new TokenGetListOutputDto
        {
            Id = token.Id,
            Name = token.Name,
            ApiKey = token.TokenKey,
            ExpireTime = token.ExpireTime,
            IsDisabled = token.IsDisabled,
            IsEnableLog = token.IsEnableLog,
            CreationTime = token.CreationTime
        };
    }

    /// <summary>
    /// 编辑Token
    /// </summary>
    [HttpPut("token")]
    public async Task UpdateAsync([FromBody] TokenUpdateInput input)
    {
        var userId = CurrentUser.GetId();

        var token = await _tokenRepository._DbQueryable
            .FirstAsync(x => x.Id == input.Id && x.UserId == userId);

        if (token is null)
        {
            throw new UserFriendlyException("Token不存在或无权限操作");
        }

        // 检查名称是否重复（排除自己）
        var exists = await _tokenRepository._DbQueryable
            .AnyAsync(x => x.UserId == userId && x.Name == input.Name && x.Id != input.Id);
        if (exists)
        {
            throw new UserFriendlyException($"名称【{input.Name}】已存在，请使用其他名称");
        }

        token.Name = input.Name;
        token.ExpireTime = input.ExpireTime;

        await _tokenRepository.UpdateAsync(token);
    }

    /// <summary>
    /// 删除Token
    /// </summary>
    [HttpDelete("token/{id}")]
    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();

        var token = await _tokenRepository._DbQueryable
            .FirstAsync(x => x.Id == id && x.UserId == userId);

        if (token is null)
        {
            throw new UserFriendlyException("Token不存在或无权限操作");
        }

        await _tokenRepository.DeleteAsync(token);
    }

    /// <summary>
    /// 启用Token
    /// </summary>
    [HttpPost("token/{id}/enable")]
    public async Task EnableAsync(Guid id)
    {
        var userId = CurrentUser.GetId();

        var token = await _tokenRepository._DbQueryable
            .FirstAsync(x => x.Id == id && x.UserId == userId);

        if (token is null)
        {
            throw new UserFriendlyException("Token不存在或无权限操作");
        }

        token.Enable();
        await _tokenRepository.UpdateAsync(token);
    }

    /// <summary>
    /// 禁用Token
    /// </summary>
    [HttpPost("token/{id}/disable")]
    public async Task DisableAsync(Guid id)
    {
        var userId = CurrentUser.GetId();

        var token = await _tokenRepository._DbQueryable
            .FirstAsync(x => x.Id == id && x.UserId == userId);

        if (token is null)
        {
            throw new UserFriendlyException("Token不存在或无权限操作");
        }

        token.Disable();
        await _tokenRepository.UpdateAsync(token);
    }
}
