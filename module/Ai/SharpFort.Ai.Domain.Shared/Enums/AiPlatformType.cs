using System.ComponentModel;

namespace SharpFort.Ai.Domain.Shared.Enums;

/// <summary>
/// AI平台类型
/// </summary>
public enum AiPlatformType
{
    [Description("OpenAI")]
    OpenAI = 1,

    [Description("Azure OpenAI")]
    AzureOpenAI = 2,

    [Description("智谱AI")]
    ZhiPuAI = 3,

    [Description("Bge Embedding")]
    BgeEmbedding = 7,

    [Description("Bge Rerank")]
    BgeRerank = 8,

    [Description("Ollama")]
    Ollama = 10,

    [Description("Ollama Embedding")]
    OllamaEmbedding = 11
}
