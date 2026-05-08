#pragma warning disable CA1720 // Identifier contains type name — 'object' matches OpenAI API schema

using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.OpenAi;

public class ModelsListDto
{
    [JsonPropertyName("object")] public string @object { get; set; } = null!;

    [JsonPropertyName("data")] public List<ModelsDataDto> Data { get; set; }

    public ModelsListDto()
    {
        Data = [];
    }
}

public class ModelsDataDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    [JsonPropertyName("object")] public string @object { get; set; } = null!;

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("owned_by")] public string OwnedBy { get; set; } = null!;

    [JsonPropertyName("type")] public string Type { get; set; } = null!;
}
