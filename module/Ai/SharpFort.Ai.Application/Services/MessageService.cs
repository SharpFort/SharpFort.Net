using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using SharpFort.Ai.Application.Contracts.Dtos.ChatMessage;
using SharpFort.Ai.Domain.Entities;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class MessageService(ISqlSugarRepository<ChatMessage> repository) : ApplicationService
{
    private readonly ISqlSugarRepository<ChatMessage> _repository = repository;

    [Authorize]
    public async Task<PagedResultDto<ChatMessageDto>> GetListAsync([FromQuery] ChatMessageGetListInput input)
    {
        RefAsync<int> total = 0;
        Guid userId = CurrentUser.GetId();
        List<ChatMessage> entities = await _repository._DbQueryable
            .Where(x => x.SessionId == input.SessionId)
            .Where(x => x.UserId == userId)
            .Where(x => !x.IsHidden)
            .OrderBy(x => x.Id)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        return new PagedResultDto<ChatMessageDto>(total, entities.Adapt<List<ChatMessageDto>>());
    }

    [Authorize]
    public async Task DeleteAsync([FromQuery] ChatMessageDeleteInput input)
    {
        Guid userId = CurrentUser.GetId();

        List<ChatMessage> messages = await _repository._DbQueryable
            .Where(x => input.Ids.Contains(x.Id))
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        List<Guid> idsToHide = [.. messages.Select(x => x.Id)];

        if (input.IsDeleteSubsequent)
        {
            foreach (ChatMessage message in messages)
            {
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

        await _repository._Db.Updateable<ChatMessage>()
            .SetColumns(x => x.IsHidden)
            .Where(x => idsToHide.Contains(x.Id))
            .ExecuteCommandAsync();
    }
}
