using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Notice;

/// <summary>Notice输入创建对象</summary>
public class NoticeCreateInput
{
    public required string Title { get; set; }
    public NoticeType NoticeType { get; set; }
    public required string Content { get; set; }
    public int OrderNum { get; set; }
    public bool State { get; set; }
}
