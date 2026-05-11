using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SharpFort.WeChat.MiniProgram.HttpModels;

namespace SharpFort.WeChat.MiniProgram.Token;

internal class DefaultMinProgramToken(IOptions<WeChatMiniProgramOptions> options) : IMiniProgramToken
{
    private const string Url = "https://api.weixin.qq.com/cgi-bin/token";
    private readonly WeChatMiniProgramOptions _options = options.Value;

    public async Task<string> GetTokenAsync()
    {
        AccessTokenResponse token = await this.GetAccessToken();
        return token.access_token;
    }
    public async Task<AccessTokenResponse> GetAccessToken()
    {
        AccessTokenRequest req = new()
        {
            appid = _options.AppID,
            secret = _options.AppSecret,
            grant_type = "client_credential"
        };
        using (HttpClient httpClient = new())
        {
            string queryString = req.ToQueryString();
            UriBuilder builder = new(Url)
            {
                Query = queryString
            };
            HttpResponseMessage response = await httpClient.GetAsync(builder.ToString());

            response.EnsureSuccessStatusCode();

            AccessTokenResponse? responseBody = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
            return responseBody;
        }
    }
}