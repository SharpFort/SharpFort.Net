namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

public class LoginOutputDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
}