using Volo.Abp.Application.Dtos;
using SharpFort.Ai.Domain.Shared.Enums;

namespace SharpFort.Ai.Application.Contracts.Dtos;

public class SessionDto : FullAuditedEntityDto<Guid>
{
    public string SessionTitle { get; set; } = null!;
    public string SessionContent { get; set; } = null!;
    public string Remark { get; set; } = null!;

    /// <summary>
    /// 会话类型
    /// </summary>
    public SessionType SessionType { get; set; }
}
