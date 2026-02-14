using System.Reflection;
using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

/// <summary>
/// 消息创建结果输出
/// </summary>
public class MessageCreatedOutput
{
    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonIgnore]
    public ChatMessageTypeEnum TypeEnum { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public string Type => TypeEnum.ToString();

    /// <summary>
    /// 消息ID
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// 消息创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum ChatMessageTypeEnum
{
    /// <summary>
    /// 用户消息
    /// </summary>
    UserMessage,

    /// <summary>
    /// 系统消息
    /// </summary>
    SystemMessage
}
