namespace SharpFort.WeChat.MiniProgram.Token;

public interface IMiniProgramToken
{
    public Task<string> GetTokenAsync();
}