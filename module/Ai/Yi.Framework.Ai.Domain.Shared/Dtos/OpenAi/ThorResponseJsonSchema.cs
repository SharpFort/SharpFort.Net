using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

public class ThorResponseJsonSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }

    [JsonPropertyName("schema")]
    public object Schema { get; set; }
}
