using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Images;

public record ImageCreateResponse : ThorBaseResponse
{
    [JsonPropertyName("data")] public List<ImageDataResult> Results { get; set; }

    [JsonPropertyName("usage")] public ThorUsageResponse? Usage { get; set; } = new();
    


    public record ImageDataResult
    {
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("b64_json")] public string B64 { get; set; }
        [JsonPropertyName("revised_prompt")] public string RevisedPrompt { get; set; }
    }
}
