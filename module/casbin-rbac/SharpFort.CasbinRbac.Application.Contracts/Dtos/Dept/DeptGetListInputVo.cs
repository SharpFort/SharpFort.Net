using SharpFort.Ddd;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Dept
{
    public class DeptGetListInputVo : PagedAllResultRequestDto
    {
        public Guid Id { get; set; }
        public bool? State { get; set; }
        public string? DeptName { get; set; }
        public string? DeptCode { get; set; }
        public string? Leader { get; set; }

    }
}
