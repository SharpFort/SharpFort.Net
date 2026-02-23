using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

public sealed class ThorChatAudioRequest
{
    [JsonPropertyName("voice")]
    public string? Voice { get; set; }
    
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
