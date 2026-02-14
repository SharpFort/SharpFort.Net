using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Yi.Framework.Ai.Application.Contracts.Dtos;
using Yi.Framework.Ai.Domain.Entities;
using Yi.Framework.SqlSugarCore.Abstractions;

namespace Yi.Framework.Ai.Application.Services;

public class MessageService : ApplicationService
{
    private readonly ISqlSugarRepository<ChatMessage> _repository;

    public MessageService(ISqlSugarRepository<ChatMessage> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 查询消息
    /// 需要会话id
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Authorize]
    public async Task<PagedResultDto<MessageDto>> GetListAsync([FromQuery]MessageGetListInput input)
    {
        RefAsync<int> total = 0;
        var userId = CurrentUser.GetId();
        var entities = await _repository._DbQueryable
            .Where(x => x.SessionId == input.SessionId)
            .Where(x=>x.UserId == userId)
            .Where(x => !x.IsHidden)
            .OrderBy(x => x.Id)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<MessageDto>(total, entities.Adapt<List<MessageDto>>());
    }

    /// <summary>
    /// 删除消息（软删除，标记为隐藏）
    /// </summary>
    /// <param name="input">删除参数，包含消息Id列表和是否删除后续消息的开关</param>
    [Authorize]
    public async Task DeleteAsync([FromQuery] MessageDeleteInput input)
    {
        var userId = CurrentUser.GetId();

        // 获取要删除的消息
        var messages = await _repository._DbQueryable
            .Where(x => input.Ids.Contains(x.Id))
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        // 标记当前消息为隐藏
        var idsToHide = messages.Select(x => x.Id).ToList();

        // 如果需要删除后续消息
        if (input.IsDeleteSubsequent)
        {
            foreach (var message in messages)
            {
                // 获取同一会话中时间大于当前消息的所有消息Id
                var subsequentIds = await _repository._DbQueryable
                    .Where(x => x.SessionId == message.SessionId)
                    .Where(x => x.UserId == userId)
                    .Where(x => x.CreationTime > message.CreationTime)
                    .Where(x => !x.IsHidden)
                    .Select(x => x.Id)
                    .ToListAsync();

                idsToHide.AddRange(subsequentIds);
            }

            idsToHide = idsToHide.Distinct().ToList();
        }

        // 批量更新为隐藏状态
        await _repository._Db.Updateable<ChatMessage>()
            .SetColumns(x => x.IsHidden == true)
            .Where(x => idsToHide.Contains(x.Id))
            .ExecuteCommandAsync();
    }
}
