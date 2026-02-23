using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

public record ThorBaseResponse
{
    /// <summary>
    /// 对象类型
    /// </summary>
    [JsonPropertyName("object")]
    public string? ObjectTypeName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool Successful => Error == null;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("error")]
    public ThorError? Error { get; set; }
}


