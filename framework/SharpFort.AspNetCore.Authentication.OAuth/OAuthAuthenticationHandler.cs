using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharpFort.AspNetCore.Authentication.OAuth
{
    public abstract class OauthAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions> where TOptions : AuthenticationSchemeOptions, new()
    {
        public abstract string AuthenticationSchemeNmae { get; }

        public OauthAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory httpClientFactory) : base(options, logger, encoder)
        {
            HttpClientFactory = httpClientFactory;
            HttpClient = HttpClientFactory.CreateClient();
        }


        protected IHttpClientFactory HttpClientFactory { get; }

        protected HttpClient HttpClient { get; }



        /// <summary>
        /// 生成认证票据
        /// </summary>
        /// <returns></returns>
        private AuthenticationTicket TicketConver(List<Claim> claims)
        {
            ClaimsIdentity claimsIdentity = new(claims.ToArray(), AuthenticationSchemeNmae);
            ClaimsPrincipal principal = new(claimsIdentity);
            return new AuthenticationTicket(principal, AuthenticationSchemeNmae);
        }

        protected async Task<THttpModel> SendHttpRequestAsync<THttpModel>(string url, IEnumerable<KeyValuePair<string, string?>> query, HttpMethod? httpMethod = null)
        {
            httpMethod ??= HttpMethod.Get;

            string queryUrl = QueryHelpers.AddQueryString(url, query);
            HttpResponseMessage response = httpMethod == HttpMethod.Get
                ? await HttpClient.GetAsync(queryUrl)
                : await HttpClient.PostAsync(queryUrl, null);

            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"授权服务器请求错误,请求地址:{queryUrl},错误信息：{content}");
            }
            VerifyErrResponse(content);
            THttpModel? model = Newtonsoft.Json.JsonConvert.DeserializeObject<THttpModel>(content);
            return model!;
        }

        protected virtual void VerifyErrResponse(string content)
        {
            AuthticationErrCodeModel.VerifyErrResponse(content);
        }

        protected abstract Task<List<Claim>> GetAuthTicketAsync(string code);


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Query.ContainsKey("code"))
            {
                return AuthenticateResult.Fail("回调未包含code参数");
            }
            string code = Context.Request.Query["code"].ToString();

            List<Claim> authTicket;
            try
            {
                authTicket = await GetAuthTicketAsync(code);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex.Message ?? "未知错误");
            }
            AuthenticateResult result = AuthenticateResult.Success(TicketConver(authTicket));
            return result;
        }
    }
}
