using SharpFort.Ddd.Application.Contracts;
using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Notice
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
