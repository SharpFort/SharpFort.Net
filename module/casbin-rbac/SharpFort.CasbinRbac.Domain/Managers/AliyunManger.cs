using AlibabaCloud.SDK.Dysmsapi20170525;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Services;
using SharpFort.CasbinRbac.Domain.Shared.Options;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    public partial class AliyunManger(ILogger<AliyunManger> logger, IOptions<AliyunOptions> options) : DomainService, IAliyunManger
    {
        private readonly ILogger<AliyunManger> _logger = logger;
        private AliyunOptions Options { get; set; } = options.Value;

        private Client CreateClient()
        {
            AlibabaCloud.OpenApiClient.Models.Config config = new()
            {
                // 必填，您的 AccessKey ID
                AccessKeyId = Options.AccessKeyId,
                // 必填，您的 AccessKey Secret
                AccessKeySecret = Options.AccessKeySecret,
                // 访问的域名
                Endpoint = "dysmsapi.aliyuncs.com"
            };
            return new Client(config);
        }


        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="phoneNumbers"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task SendSmsAsync(string phoneNumbers, string code)
        {

            try
            {
                Client _aliyunClient = CreateClient();
                SendSmsRequest sendSmsRequest = new()
                {
                    PhoneNumbers = phoneNumbers,
                    SignName = Options.Sms.SignName,
                    TemplateCode = Options.Sms.TemplateCode,
                    TemplateParam = System.Text.Json.JsonSerializer.Serialize(new { code })
                };

                SendSmsResponse response = await _aliyunClient.SendSmsAsync(sendSmsRequest);
            }

            catch (Exception _error)
            {
                LogAliyunSmsError(_error, _error.Message);
                throw new UserFriendlyException("阿里云短信发送错误:" + _error.Message);
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "阿里云短信发送错误: {ErrorMessage}")]
        private partial void LogAliyunSmsError(Exception ex, string errorMessage);
    }

    public interface IAliyunManger
    {
        Task SendSmsAsync(string phoneNumbers, string code);
    }
}
