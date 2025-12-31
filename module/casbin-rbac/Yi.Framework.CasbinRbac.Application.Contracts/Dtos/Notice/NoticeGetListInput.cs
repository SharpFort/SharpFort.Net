using Yi.Framework.Ddd.Application.Contracts;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Notice
{
    /// <summary>
    /// 查询参数
    /// </summary>
    public class NoticeGetListInput : PagedAllResultRequestDto
    {
        public string? Title { get; set; }
        public NoticeType? NoticeType { get; set; }
    }
}
