using Microsoft.AspNetCore.Authentication.OAuth;

namespace SharpFort.AspNetCore.Authentication.OAuth
{
    public class AuthenticationOAuthOptions : OAuthOptions
    {

        public string RedirectUri { get; set; } = null!;
    }
}
