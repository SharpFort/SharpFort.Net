using System.ComponentModel.DataAnnotations;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class MessageGetListInput:PagedAllResultRequestDto
{
    [Required]
    public Guid SessionId { get; set; }
}

public class MessageDeleteInput
{
    /// <summary>
    /// 要删除的消息Id列表
    /// </summary>
    [Required]
    public List<Guid> Ids { get; set; } = new();

    /// <summary>
    /// 是否同时隐藏后续消息（同一会话中时间大于当前消息的所有消息）
    /// </summary>
    public bool IsDeleteSubsequent { get; set; } = false;
}
