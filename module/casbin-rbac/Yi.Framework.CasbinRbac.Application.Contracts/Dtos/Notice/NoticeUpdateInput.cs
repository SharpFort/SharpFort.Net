using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Notice
{
    public class NoticeUpdateInput
    {
        public string? Title { get; set; }
        public NoticeType? NoticeType { get; set; }
        public string? Content { get; set; }
        public int? OrderNum { get; set; }
        public bool? State { get; set; }
    }
}
