using Volo.Abp.Application.Dtos;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

public class AuthOutputDto : EntityDto<Guid>
{
    public Guid UserId { get; set; }
    public required string OpenId { get; set; }
    public required string Name { get; set; }
    public required string AuthType { get; set; }
    public DateTime CreationTime { get; set; }
}
