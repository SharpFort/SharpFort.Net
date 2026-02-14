using System.Text.Json.Serialization;

namespace Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Responses;

public class OpenAiResponsesOutput
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("object")]
    public string? Object { get; set; }
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("error")]
    public dynamic? Error { get; set; }
    [JsonPropertyName("incomplete_details")]
    public dynamic? IncompleteDetails { get; set; }
    [JsonPropertyName("instructions")]
    public dynamic? Instructions { get; set; }
    [JsonPropertyName("max_output_tokens")]
    public dynamic? MaxOutputTokens { get; set; }
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    // output 是复杂对象
    [JsonPropertyName("output")]
    public List<dynamic>? Output { get; set; }
    [JsonPropertyName("parallel_tool_calls")]
    public bool ParallelToolCalls { get; set; }
    [JsonPropertyName("previous_response_id")]
    public dynamic? PreviousResponseId { get; set; }
    [JsonPropertyName("reasoning")]
    public dynamic? Reasoning { get; set; }
    [JsonPropertyName("store")]
    public bool Store { get; set; }
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
    [JsonPropertyName("text")]
    public dynamic? Text { get; set; }
    [JsonPropertyName("tool_choice")]
    public string? ToolChoice { get; set; }
    [JsonPropertyName("tools")]
    public List<dynamic>? Tools { get; set; }
    [JsonPropertyName("top_p")]
    public double TopP { get; set; }
    [JsonPropertyName("truncation")]
    public string? Truncation { get; set; }
    // usage 为唯一强类型
    [JsonPropertyName("usage")]
    public OpenAiResponsesUsageOutput? Usage { get; set; }
    [JsonPropertyName("user")]
    public dynamic? User { get; set; }
    [JsonPropertyName("metadata")]
    public dynamic? Metadata { get; set; }
    
    public void SupplementalMultiplier(decimal multiplier)
    {
        if (this.Usage is not null)
        {
            this.Usage.InputTokens =
                (int)Math.Round((this.Usage?.InputTokens ?? 0) * multiplier);

            this.Usage.OutputTokens =
                (int)Math.Round((this.Usage?.OutputTokens ?? 0) * multiplier);
        }
    }
}

public class OpenAiResponsesUsageOutput
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }
    [JsonPropertyName("input_tokens_details")]
    public OpenAiResponsesInputTokensDetails? InputTokensDetails { get; set; }
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
    [JsonPropertyName("output_tokens_details")]
    public OpenAiResponsesOutputTokensDetails? OutputTokensDetails { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
public class OpenAiResponsesInputTokensDetails
{
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }
}
public class OpenAiResponsesOutputTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int ReasoningTokens { get; set; }
}
