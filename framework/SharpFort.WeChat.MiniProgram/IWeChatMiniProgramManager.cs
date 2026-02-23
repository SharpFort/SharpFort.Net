using SharpFort.WeChat.MiniProgram.HttpModels;

namespace SharpFort.WeChat.MiniProgram;

public interface IWeChatMiniProgramManager
{
    /// <summary>
    /// 获取用户openid
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<Code2SessionResponse> Code2SessionAsync(Code2SessionInput input);

    /// <summary>
    /// 向用户发送订阅消息，要openid
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task SendSubscribeNoticeAsync(SubscribeNoticeInput input);
}