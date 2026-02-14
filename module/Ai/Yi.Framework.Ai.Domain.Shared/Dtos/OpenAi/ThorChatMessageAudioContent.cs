
using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

public sealed class ThorChatMessageAudioContent
{
    [JsonPropertyName("data")]
    public string? Data { get; set; }
    
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
