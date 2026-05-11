namespace SharpFort.WeChat.MiniProgram.Token;

public interface IMiniProgramToken
{
    Task<string> GetTokenAsync();
}