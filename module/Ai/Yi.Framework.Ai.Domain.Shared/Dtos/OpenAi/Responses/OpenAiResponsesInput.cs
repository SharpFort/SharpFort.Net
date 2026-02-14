using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Responses;

public class OpenAiResponsesInput
{
    [JsonPropertyName("stream")] public bool? Stream { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; }
    [JsonPropertyName("input")] public JsonElement Input { get; set; }

    [JsonPropertyName("max_output_tokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("max_tool_calls")] public JsonElement? MaxToolCalls { get; set; }
    [JsonPropertyName("instructions")] public string? Instructions { get; set; }
    [JsonPropertyName("metadata")] public JsonElement? Metadata { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    public bool? ParallelToolCalls { get; set; }

    [JsonPropertyName("previous_response_id")]
    public string? PreviousResponseId { get; set; }

    [JsonPropertyName("prompt")] public JsonElement? Prompt { get; set; }
    [JsonPropertyName("prompt_cache_key")] public string? PromptCacheKey { get; set; }

    [JsonPropertyName("prompt_cache_retention")]
    public string? PromptCacheRetention { get; set; }

    [JsonPropertyName("reasoning")] public JsonElement? Reasoning { get; set; }

    [JsonPropertyName("safety_identifier")]
    public string? SafetyIdentifier { get; set; }

    [JsonPropertyName("service_tier")] public string? ServiceTier { get; set; }
    [JsonPropertyName("store")] public bool? Store { get; set; }
    [JsonPropertyName("stream_options")] public JsonElement? StreamOptions { get; set; }
    [JsonPropertyName("temperature")] public decimal? Temperature { get; set; }
    [JsonPropertyName("text")] public JsonElement? Text { get; set; }
    [JsonPropertyName("tool_choice")] public JsonElement? ToolChoice { get; set; }
    [JsonPropertyName("tools")] public JsonElement? Tools { get; set; }
    [JsonPropertyName("top_logprobs")] public int? TopLogprobs { get; set; }
    [JsonPropertyName("top_p")] public decimal? TopP { get; set; }
    [JsonPropertyName("truncation")] public string? Truncation { get; set; }
}
