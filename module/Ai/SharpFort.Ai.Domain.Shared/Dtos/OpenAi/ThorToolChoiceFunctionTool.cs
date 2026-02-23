using System.Text.Json.Serialization;

namespace SharpFort.Ai.Domain.Shared.Dtos.OpenAi
{
    public class ThorToolChoiceFunctionTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
