namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

public class PhoneCaptchaImageDto
{
    public required string Phone { get; set; }
    public required string Uuid { get; set; }
    public required string Code { get; set; }
}
