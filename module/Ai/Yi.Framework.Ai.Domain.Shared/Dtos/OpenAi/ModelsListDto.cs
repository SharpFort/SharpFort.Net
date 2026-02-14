using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

public class ModelsListDto
{
    [JsonPropertyName("object")] public string @object { get; set; }

    [JsonPropertyName("data")] public List<ModelsDataDto> Data { get; set; }
    
    public ModelsListDto()
    {
        Data = new();
    }
}

public class ModelsDataDto
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("object")] public string @object { get; set; }

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("owned_by")] public string OwnedBy { get; set; }
    
    [JsonPropertyName("type")] public string Type { get; set; }
}
