using System.Reflection;
using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Application.Contracts.Dtos.Chat;

public class AgentResultOutput
{
    /// <summary>
    /// 类型
    /// </summary>
    [JsonIgnore]
    public AgentResultTypeEnum TypeEnum { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string Type => TypeEnum.GetJsonName();
    
    /// <summary>
    /// 内容载体
    /// </summary>
    public object Content { get; set; }
}

public enum AgentResultTypeEnum
{
    /// <summary>
    /// 文本内容
    /// </summary>
    [JsonPropertyName("text")]
    Text,
    /// <summary>
    /// 工具调用中
    /// </summary>
    [JsonPropertyName("toolCalling")]
    ToolCalling,
    
    /// <summary>
    /// 工具调用完成
    /// </summary>
    [JsonPropertyName("toolCalled")]
    ToolCalled,
    
    /// <summary>
    /// 用量
    /// </summary>
    [JsonPropertyName("usage")]
    Usage,
    
    /// <summary>
    /// 工具调用用量
    /// </summary>
    [JsonPropertyName("toolCallUsage")]
    ToolCallUsage
}

public static class AgentResultTypeEnumExtensions
{
    public static string GetJsonName(this AgentResultTypeEnum value)
    {
        var member = typeof(AgentResultTypeEnum).GetMember(value.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attr?.Name ?? value.ToString();
    }
}
