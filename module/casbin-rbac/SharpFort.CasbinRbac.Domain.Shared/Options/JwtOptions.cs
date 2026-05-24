namespace SharpFort.CasbinRbac.Domain.Shared.Options
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = "ccnetcore.com";

        public string Audience { get; set; } = "https//ccnetcore.com";

        public string SecurityKey { get; set; } = string.Empty;

        public long ExpiresMinuteTime { get; set; } = 120;
    }
}
