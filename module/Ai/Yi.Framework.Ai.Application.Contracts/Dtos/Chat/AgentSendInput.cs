namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

public class AgentSendInput
{
    /// <summary>
    /// 会话id
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// 用户内容
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// api密钥Id
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// 模型id
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 已选择工具
    /// </summary>
    public List<string> Tools { get; set; }
}
