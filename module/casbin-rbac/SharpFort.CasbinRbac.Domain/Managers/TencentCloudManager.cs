using TencentCloud.Common.Profile;
using TencentCloud.Common;
using TencentCloud.Sms.V20210111.Models;
using TencentCloud.Sms.V20210111;
using Volo.Abp.Domain.Services;
using Microsoft.Extensions.Logging;

namespace SharpFort.CasbinRbac.Domain.Managers
{
    public partial class TencentCloudManager : DomainService
    {
        private readonly ILogger<TencentCloudManager> _logger;
        public TencentCloudManager(ILogger<TencentCloudManager> logger)
        {
            _logger= logger;
        }

        public async Task SendSmsAsync()
        {

            try
            {
                // 实例化一个认证对象，入参需要传入腾讯云账户 SecretId 和 SecretKey，此处还需注意密钥对的保密
                // 代码泄露可能会导致 SecretId 和 SecretKey 泄露，并威胁账号下所有资源的安全性。以下代码示例仅供参考，建议采用更安全的方式来使用密钥，请参见：https://cloud.tencent.com/document/product/1278/85305
                // 密钥可前往官网控制台 https://console.cloud.tencent.com/cam/capi 进行获取
                Credential cred = new Credential
                {
                    SecretId = "SecretId",
                    SecretKey = "SecretKey"
                };
                // 实例化一个client选项，可选的，没有特殊需求可以跳过
                ClientProfile clientProfile = new ClientProfile();
                // 实例化一个http选项，可选的，没有特殊需求可以跳过
                HttpProfile httpProfile = new HttpProfile();
                httpProfile.Endpoint = ("sms.tencentcloudapi.com");
                clientProfile.HttpProfile = httpProfile;

                // 实例化要请求产品的client对象,clientProfile是可选的
                SmsClient client = new SmsClient(cred, "", clientProfile);
                // 实例化一个请求对象,每个接口都会对应一个request对象
                SendSmsRequest req = new SendSmsRequest();

                // 返回的resp是一个SendSmsResponse的实例，与请求对象对应
                SendSmsResponse resp = await client.SendSms(req);
                // 输出json格式的字符串回包
                if (_logger.IsEnabled(LogLevel.Information))
                {
#pragma warning disable CA1873 // IsEnabled 已保护此调用
                    LogSmsResponse(AbstractModel.ToJsonString(resp));
#pragma warning restore CA1873
                }
            }
            catch (Exception e)
            {
                LogSmsError(e);
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "腾讯云Sms返回：{Response}")]
        private partial void LogSmsResponse(string response);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "腾讯云短信发送失败")]
        private partial void LogSmsError(Exception ex);
    }
}
