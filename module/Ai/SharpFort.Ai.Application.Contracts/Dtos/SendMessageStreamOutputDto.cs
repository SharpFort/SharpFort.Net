#pragma warning disable CA1720 // Identifier contains type name — 'Object' matches OpenAI API schema

namespace SharpFort.Ai.Application.Contracts.Dtos;

public class SendMessageStreamOutputDto
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// 对象类型
    /// </summary>
    public string Object { get; set; } = null!;

    /// <summary>
    /// 创建时间，Unix时间戳格式
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// 选择项列表
    /// </summary>
    public List<Choice> Choices { get; set; } = new List<Choice>();

    /// <summary>
    /// 系统指纹（可能为空）
    /// </summary>
    public string SystemFingerprint { get; set; } = null!;

    /// <summary>
    /// 使用情况信息
    /// </summary>
    public Usage Usage { get; set; } = null!;
}

/// <summary>
/// 选择项类，表示模型返回的一个选择
/// </summary>
public class Choice
{
    /// <summary>
    /// 选择索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 变化内容，包括内容字符串和角色
    /// </summary>
    public Delta Delta { get; set; } = null!;

    /// <summary>
    /// 结束原因，可能为空
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// 内容过滤结果
    /// </summary>
    public ContentFilterResults ContentFilterResults { get; set; } = null!;
}

/// <summary>
/// 变化内容
/// </summary>
public class Delta
{
    /// <summary>
    /// 内容文本
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// 角色，例如"assistant"
    /// </summary>
    public string Role { get; set; } = null!;
}

/// <summary>
/// 内容过滤结果
/// </summary>
public class ContentFilterResults
{
    public FilterStatus Hate { get; set; } = null!;
    public FilterStatus SelfHarm { get; set; } = null!;
    public FilterStatus Sexual { get; set; } = null!;
    public FilterStatus Violence { get; set; } = null!;
    public FilterStatus Jailbreak { get; set; } = null!;
    public FilterStatus Profanity { get; set; } = null!;
}

/// <summary>
/// 过滤状态，表示是否经过过滤以及检测是否命中
/// </summary>
public class FilterStatus
{
    /// <summary>
    /// 是否被过滤
    /// </summary>
    public bool Filtered { get; set; }

    /// <summary>
    /// 是否检测到该类型（例如 Jailbreak 中存在此字段）
    /// </summary>
    public bool? Detected { get; set; }
}

/// <summary>
/// 使用情况，记录 token 数量等信息
/// </summary>
public class Usage
{
    /// <summary>
    /// 提示词数量
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// 补全词数量
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总的 Token 数量
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 提示词详细信息
    /// </summary>
    public PromptTokensDetails PromptTokensDetails { get; set; } = null!;

    /// <summary>
    /// 补全文字详细信息
    /// </summary>
    public CompletionTokensDetails CompletionTokensDetails { get; set; } = null!;
}

/// <summary>
/// 提示词相关 token 详细信息
/// </summary>
public class PromptTokensDetails
{
    public int AudioTokens { get; set; }
    public int CachedTokens { get; set; }
}

/// <summary>
/// 补全相关 token 详细信息
/// </summary>
public class CompletionTokensDetails
{
    public int AudioTokens { get; set; }

    public int ReasoningTokens { get; set; }

    public int AcceptedPredictionTokens { get; set; }

    public int RejectedPredictionTokens { get; set; }
}
