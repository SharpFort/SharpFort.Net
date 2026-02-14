using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;

public sealed class AnthropicCacheControl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } 
}
