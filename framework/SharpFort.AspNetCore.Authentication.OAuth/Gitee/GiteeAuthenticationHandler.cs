using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static SharpFort.AspNetCore.Authentication.OAuth.Gitee.GiteeAuthenticationConstants;

namespace SharpFort.AspNetCore.Authentication.OAuth.Gitee
{
    public class GiteeAuthenticationHandler(IOptionsMonitor<GiteeAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory httpClientFactory) : OauthAuthenticationHandler<GiteeAuthenticationOptions>(options, logger, encoder, httpClientFactory)
    {
        public override string AuthenticationSchemeNmae => GiteeAuthenticationDefaults.AuthenticationScheme;

        protected override async Task<List<Claim>> GetAuthTicketAsync(string code)
        {
            //获取 accessToken
            List<KeyValuePair<string, string?>> tokenQueryKv =
            [
                new("grant_type","authorization_code"),
                new("client_id",Options.ClientId),
                new("client_secret",Options.ClientSecret),
                new("redirect_uri",Options.RedirectUri),
                new("code",code)
            ];
            GiteeAuthticationcationTokenResponse tokenModel = await SendHttpRequestAsync<GiteeAuthticationcationTokenResponse>(GiteeAuthenticationDefaults.TokenEndpoint, tokenQueryKv, HttpMethod.Post);

            //获取 userInfo
            List<KeyValuePair<string, string?>> userInfoQueryKv =
            [
                new("access_token",tokenModel.access_token),
            ];
            GiteeAuthticationcationUserInfoResponse userInfoMdoel = await SendHttpRequestAsync<GiteeAuthticationcationUserInfoResponse>(GiteeAuthenticationDefaults.UserInformationEndpoint, userInfoQueryKv);

            List<Claim> claims =
            [
                new Claim(Claims.AvatarUrl, userInfoMdoel.avatar_url),
                new Claim(Claims.Url, userInfoMdoel.url),

                new Claim(AuthenticationConstants.OpenId,userInfoMdoel.id.ToString(CultureInfo.InvariantCulture)),
                new Claim(AuthenticationConstants.Name, userInfoMdoel.name),
                new Claim(AuthenticationConstants.AccessToken, tokenModel.access_token)
            ];
            return claims;
        }
    }
}
