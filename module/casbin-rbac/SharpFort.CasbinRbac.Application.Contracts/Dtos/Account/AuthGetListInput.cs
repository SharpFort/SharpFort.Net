using SharpFort.Ddd.Application.Contracts;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account
{
    public class AuthGetListInput:PagedAllResultRequestDto
    {
        public Guid? UserId { get; set; }

        public string? OpenId { get; set; }

        public string? AuthType { get; set; }
    }
}
