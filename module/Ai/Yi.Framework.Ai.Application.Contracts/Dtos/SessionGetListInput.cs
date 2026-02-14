using Yi.Framework.Ai.Domain.Shared.Enums;
using Yi.Framework.Ddd.Application.Contracts;

namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class SessionGetListInput : PagedAllResultRequestDto
{
    public string? SessionTitle { get; set; }

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionTypeEnum? SessionType { get; set; }
}
