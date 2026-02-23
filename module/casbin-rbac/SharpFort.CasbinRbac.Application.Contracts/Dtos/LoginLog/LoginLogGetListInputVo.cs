using SharpFort.Ddd;
using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.LoginLog
{
    public class LoginLogGetListInputVo : PagedAllResultRequestDto
    {
        public string? LoginUser { get; set; }

        public string? LoginIp { get; set; }
    }
}
