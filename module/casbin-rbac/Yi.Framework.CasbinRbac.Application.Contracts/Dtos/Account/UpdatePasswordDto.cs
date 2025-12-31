namespace Yi.Framework.CasbinRbac.Application.Contracts.Dtos.Account
{
    public class UpdatePasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
    }
}
