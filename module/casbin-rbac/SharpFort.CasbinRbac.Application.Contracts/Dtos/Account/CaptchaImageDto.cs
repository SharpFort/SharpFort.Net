namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Account;

public class CaptchaImageDto
{
    public Guid Uuid { get; set; }
    public required byte[] Img { get; set; }
    public bool IsEnableCaptcha { get; set; }
}
