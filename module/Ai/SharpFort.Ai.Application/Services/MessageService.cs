using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class MessageService(ISqlSugarRepository<ChatMessage> repository) : ApplicationService
{
    private readonly ISqlSugarRepository<ChatMessage> _repository = repository;

    /// <summary>
    /// 查询消息
    /// 需要会话id
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [Authorize]
    public async Task<PagedResultDto<MessageDto>> GetListAsync([FromQuery] MessageGetListInput input)
    {
        RefAsync<int> total = 0;
        Guid userId = CurrentUser.GetId();
        List<ChatMessage> entities = await _repository._DbQueryable
            .Where(x => x.SessionId == input.SessionId)
            .Where(x => x.UserId == userId)
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
        Guid userId = CurrentUser.GetId();

        // 获取要删除的消息
        List<ChatMessage> messages = await _repository._DbQueryable
            .Where(x => input.Ids.Contains(x.Id))
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        // 标记当前消息为隐藏
        List<Guid> idsToHide = [.. messages.Select(x => x.Id)];

        // 如果需要删除后续消息
        if (input.IsDeleteSubsequent)
        {
            foreach (ChatMessage message in messages)
            {
                // 获取同一会话中时间大于当前消息的所有消息Id
                List<Guid> subsequentIds = await _repository._DbQueryable
                    .Where(x => x.SessionId == message.SessionId)
                    .Where(x => x.UserId == userId)
                    .Where(x => x.CreationTime > message.CreationTime)
                    .Where(x => !x.IsHidden)
                    .Select(x => x.Id)
                    .ToListAsync();

                idsToHide.AddRange(subsequentIds);
            }

            idsToHide = [.. idsToHide.Distinct()];
        }

        // 批量更新为隐藏状态
        await _repository._Db.Updateable<ChatMessage>()
            .SetColumns(x => x.IsHidden == true)
            .Where(x => idsToHide.Contains(x.Id))
            .ExecuteCommandAsync();
    }
}
