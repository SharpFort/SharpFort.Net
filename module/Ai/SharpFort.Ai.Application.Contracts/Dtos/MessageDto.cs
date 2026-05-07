using Volo.Abp.Application.Dtos;

namespace SharpFort.Ai.Application.Contracts.Dtos;

public class MessageDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Content { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ModelId { get; set; } = null!;
    public string Remark { get; set; } = null!;
}
