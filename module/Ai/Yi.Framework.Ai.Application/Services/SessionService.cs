using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

public class SessionService : CrudAppService<ChatSession, SessionDto, Guid,SessionGetListInput,SessionCreateAndUpdateInput>
{
    private readonly ISqlSugarRepository<ChatSession, Guid> _repository;
    public readonly ISqlSugarRepository<ChatMessage, Guid> _messageRepository;
    public SessionService(ISqlSugarRepository<ChatSession, Guid> repository, ISqlSugarRepository<ChatMessage, Guid> messageRepository) : base(repository)
    {
        _repository = repository;
        _messageRepository = messageRepository;
    }

    /// <summary>
    /// 创建会话
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Authorize]
    public override async Task<SessionDto> CreateAsync(SessionCreateAndUpdateInput input)
    {
        var entity = await MapToEntityAsync(input);
        entity.UserId = CurrentUser.GetId();
        await _repository.InsertAsync(entity);
        return entity.Adapt<SessionDto>();
    }

    /// <summary>
    /// 详情会话
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize]
    public override Task<SessionDto> GetAsync(Guid id)
    {
        return base.GetAsync(id);
    }

    /// <summary>
    /// 编辑会话
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [Authorize]
    public override Task<SessionDto> UpdateAsync(Guid id, SessionCreateAndUpdateInput input)
    {
        return base.UpdateAsync(id, input);
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize]
    public override async Task DeleteAsync(Guid id)
    {
        await base.DeleteAsync(id);
        //对应的消息一起删除
        await _messageRepository.DeleteAsync(x => x.SessionId == id);
    }

    /// <summary>
    /// 查询会话
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Authorize]
    public override async Task<PagedResultDto<SessionDto>> GetListAsync(SessionGetListInput input)
    {
        RefAsync<int> total = 0;
        var userId = CurrentUser.GetId();
        var entities = await _repository._DbQueryable
            .Where(x => x.UserId == userId)
            .WhereIF(input.SessionType.HasValue, x => x.SessionType == input.SessionType!.Value)
            .OrderByDescending(x => x.Id)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<SessionDto>(total, entities.Adapt<List<SessionDto>>());
    }
}
