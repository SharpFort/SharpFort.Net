using Volo.Abp.Application.Dtos;
using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Notice;

public class NoticeGetOutputDto : EntityDto<Guid>
{
    public required string Title { get; set; }
    public NoticeType NoticeType { get; set; }
    public required string Content { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public Guid? LastModifierId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public int OrderNum { get; set; }
    public bool State { get; set; }
}
