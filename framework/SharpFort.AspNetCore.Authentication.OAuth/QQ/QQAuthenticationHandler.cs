using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static SharpFort.AspNetCore.Authentication.OAuth.QQ.QQAuthenticationConstants;

namespace SharpFort.AspNetCore.Authentication.OAuth.QQ
{
    public class QQAuthenticationHandler(IOptionsMonitor<QQAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory httpClientFactory) : OauthAuthenticationHandler<QQAuthenticationOptions>(options, logger, encoder, httpClientFactory)
    {
        public override string AuthenticationSchemeNmae => QQAuthenticationDefaults.AuthenticationScheme;

        protected override async Task<List<Claim>> GetAuthTicketAsync(string code)
        {

            //获取 accessToken
            var tokenQueryKv = new List<KeyValuePair<string, string?>>()
            {
                new("grant_type","authorization_code"),
                new("client_id",Options.ClientId),
                new("client_secret",Options.ClientSecret),
                new("redirect_uri",Options.RedirectUri),
                new("fmt","json"),
                new("need_openid","1"),
                new("code",code)
            };
            var tokenModel = await SendHttpRequestAsync<QQAuthticationcationTokenResponse>(QQAuthenticationDefaults.TokenEndpoint, tokenQueryKv);



            //获取 userInfo
            var userInfoQueryKv = new List<KeyValuePair<string, string?>>()
            {
                new("access_token",tokenModel.access_token),
                new("oauth_consumer_key",Options.ClientId),
                new("openid",tokenModel.openid),
            };

            var userInfoMdoel = await SendHttpRequestAsync<QQAuthticationcationUserInfoResponse>(QQAuthenticationDefaults.UserInformationEndpoint, userInfoQueryKv);


            List<Claim> claims =
            [

                new Claim(Claims.AvatarFullUrl, userInfoMdoel.figureurl_qq_2),
                new Claim(Claims.AvatarUrl, userInfoMdoel.figureurl_qq_1),
                new Claim(Claims.PictureFullUrl, userInfoMdoel.figureurl_2),
                new Claim(Claims.PictureMediumUrl, userInfoMdoel.figureurl_qq_1),
                new Claim(Claims.PictureUrl, userInfoMdoel.figureurl),

                new Claim(AuthenticationConstants.OpenId, tokenModel.openid),
                new Claim(AuthenticationConstants.Name, userInfoMdoel.nickname),
                new Claim(AuthenticationConstants.AccessToken, tokenModel.access_token),

            ];
            return claims;

        }
    }
}
