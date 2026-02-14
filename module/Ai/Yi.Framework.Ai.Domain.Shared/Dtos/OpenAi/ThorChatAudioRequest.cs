using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

public sealed class ThorChatAudioRequest
{
    [JsonPropertyName("voice")]
    public string? Voice { get; set; }
    
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
