using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.OpenAi.Embeddings;

public sealed class ThorEmbeddingInput
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("input")]
    public object Input { get; set; } = null!;

    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; set; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }
}

