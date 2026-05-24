using System.ComponentModel;

namespace SharpFort.Ai.Domain.Shared.Enums;

/// <summary>
/// AI模型能力类型（按功能分类）
/// </summary>
public enum AiModelType
{
    [Description("对话模型")]
    Chat = 1,

    [Description("嵌入模型")]
    Embedding = 2,

    [Description("重排模型")]
    Rerank = 4
}
