using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using SharpFort.WeChat.MiniProgram.HttpModels;
using SharpFort.WeChat.MiniProgram.Token;

namespace SharpFort.WeChat.MiniProgram;

public class WeChatMiniProgramManager(IMiniProgramToken weChatToken, IOptions<WeChatMiniProgramOptions> options) : IWeChatMiniProgramManager, ISingletonDependency
{
    private readonly IMiniProgramToken _weChatToken = weChatToken;
    private readonly WeChatMiniProgramOptions _options = options.Value;

    /// <summary>
    /// 获取用户openid
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<Code2SessionResponse> Code2SessionAsync(Code2SessionInput input)
    {
        string url = "https://api.weixin.qq.com/sns/jscode2session";
        Code2SessionRequest req = new()
        {
            js_code = input.js_code,
            secret = _options.AppSecret,
            appid = _options.AppID
        };

        using (HttpClient httpClient = new())
        {
            string queryString = req.ToQueryString();
            UriBuilder builder = new(url)
            {
                Query = queryString
            };
            HttpResponseMessage response = await httpClient.GetAsync(builder.ToString());
            Code2SessionResponse? responseBody = await response.Content.ReadFromJsonAsync<Code2SessionResponse>();

            responseBody!.ValidateSuccess();

            return responseBody!;
        }
    }



    /// <summary>
    /// 发送模板订阅消息
    /// </summary>
    /// <param name="input"></param>
    public async Task SendSubscribeNoticeAsync(SubscribeNoticeInput input)
    {
        string token = await _weChatToken.GetTokenAsync();
        string url = $"https://api.weixin.qq.com/cgi-bin/message/subscribe/send?access_token={token}";
        SubscribeNoticeRequest req = new()
        {
            touser = input.touser,
            template_id = input.template_id,
            page = input.page,
            data = input.data,
            miniprogram_state = _options.Notice?.State ?? "formal"
        };
        req.template_id ??= _options.Notice?.TemplateId!;

        using (HttpClient httpClient = new())
        {
            StringContent body = new(JsonConvert.SerializeObject(req));
            HttpResponseMessage response = await httpClient.PostAsync(url, body);
            SubscribeNoticeResponse? responseBody = await response.Content.ReadFromJsonAsync<SubscribeNoticeResponse>();
            responseBody!.ValidateSuccess();
        }
    }
}