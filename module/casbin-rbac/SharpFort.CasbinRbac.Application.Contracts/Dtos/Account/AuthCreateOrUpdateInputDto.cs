namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

public class AuthCreateOrUpdateInputDto
{
    public Guid UserId { get; set; }
    public required string OpenId { get; set; }
    public required string Name { get; set; }
    public required string AuthType { get; set; }
}